using ImageAPI.Models;
using ImageAPI.Repositories;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;

namespace ImageAPI.Services
{
    /// <summary>
    /// Service for handling image-related operations such as uploading, deleting, and resizing images.
    /// </summary>
    public class ImageService
    {
        private readonly ILogger<ImageService> _logger;
        private readonly IImageRepository _imageRepository;
        private readonly string _imageBasePath;



        public ImageService(ILogger<ImageService> logger, IImageRepository imageRepository, AppSettings appSettings)
        {
            _logger = logger;
            _imageRepository = imageRepository;
            _imageBasePath = appSettings.ImageBasePath;
        }

        /// <summary>
        /// Uploads an image to the server, saves the original image and its variations, and records metadata.
        /// </summary>
        /// <param name="file">The image file to upload.</param>
        /// <returns>The GUID representing the image.</returns>
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            try
            {
                Guid imageId = Guid.NewGuid();
                string imageDir = Path.Combine(_imageBasePath, imageId.ToString());
                Directory.CreateDirectory(imageDir);

                // Use the original file extension -- maybe we should ensure we only whitelist certain extensions?
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string originalFileName = $"original{extension}";
                string originalPath = Path.Combine(imageDir, originalFileName);

                // Load the uploaded file into a memory stream
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                // Validate image and get dimensions
                memoryStream.Position = 0;
                int originalWidth, originalHeight;
                try
                {
                    using var img = System.Drawing.Image.FromStream(memoryStream);
                    originalWidth = img.Width;
                    originalHeight = img.Height;
                }
                catch (Exception)
                {
                    throw new ArgumentException("The uploaded file is not a valid image.");
                }

                await SaveToDisk(originalPath, memoryStream);

                // Also save a copy with a similar nomenclature as variations (ex 1080px.png)

                var originalVariationPath = Path.Combine(imageDir, $"{originalHeight}{extension}");
                await SaveToDisk(originalVariationPath, memoryStream);


                // Determine aspect ratio string and value
                string aspectRatioStr = $"{originalWidth}:{originalHeight}";
                double aspectRatioVal = Math.Round((double)originalWidth / originalHeight, 2);

                // Load aspect ratios from file
                string aspectJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Config", "aspect_ratios.json");
                var ratioService = new AspectRatioResolutionService();
                var allRatios = ratioService.LoadAspectRatiosFromFile(aspectJsonPath);

                List<ImageVariation> variations = new();

                if (allRatios != null)
                {
                    foreach (var kv in allRatios.AspectRatios)
                    {
                        foreach (var res in kv.Value)
                        {
                            if (res.Height < originalHeight && !variations.Any(v => v.Height == res.Height))
                            {
                                var variation = GenerateVariation(imageDir, extension, originalPath, res.Height, originalWidth, originalHeight);
                                variations.Add(variation);
                            }
                        }
                    }
                }

                // Generate 160px thumbnail if missing
                int thumbHeight = 160;
                if (!variations.Any(v => v.Height == thumbHeight))
                {
                    var variation = GenerateVariation(imageDir, extension, originalPath, thumbHeight, originalWidth, originalHeight);
                    variations.Add(variation);
                }

                // Save metadata
                var metadata = new ImageMetadata
                {
                    Id = imageId,
                    Original = new ImageVariation(originalHeight, originalWidth, originalPath),
                    Variations = variations,
                    UploadDate = DateTime.UtcNow,
                    FileSize = file.Length,
                    FileType = file.ContentType
                };

                await _imageRepository.InsertImageAsync(metadata);
                _logger.LogInformation($"Image {imageId} uploaded with {variations.Count} variations.");

                return imageId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the original image by ID.
        /// </summary>
        /// <param name="imageId">The ID of the image.</param>
        /// <returns>The path to the original image.</returns>
        public async Task<string> GetOriginalImageAsync(Guid imageId)
        {
            try
            {
                var imageMetadata = await _imageRepository.GetImageByIdAsync(imageId);
                if (imageMetadata == null)
                    throw new ArgumentException("Image not found.");

                var originalImagePath = imageMetadata.Original.Path;

                if (!File.Exists(originalImagePath))
                    throw new InvalidOperationException("Original image file does not exist.");

                return originalImagePath;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to retrieve original image.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving the original image.");
                throw new Exception("Failed to retrieve the original image.");
            }
        }

        /// <summary>
        /// Retrieves a specific variation of an image based on the requested height. If the variation does not exist, it generates and stores the variation.
        /// </summary>
        /// <param name="imageId">The unique identifier of the image.</param>
        /// <param name="height">The requested height of the image variation. The height must not exceed the original image height.</param>
        /// <returns>A string representing the path to the generated or existing image variation file.</returns>
        public async Task<string> GetImageVariationAsync(Guid imageId, int height)
        {
            try
            {
                var imageMetadata = await _imageRepository.GetImageByIdAsync(imageId);
                if (imageMetadata == null)
                    throw new ArgumentException("Image not found.");

                var original = imageMetadata.Original;

                if (height > original.Height)
                    throw new InvalidOperationException("Requested height exceeds original image height.");

                // Check if variation already exists
                var existing = imageMetadata.Variations
                    .FirstOrDefault(v => v.Height == height);

                if (existing != null)
                    return existing.Path;


                string extension = Path.GetExtension(original.Path);
                string imageDir = Path.GetDirectoryName(original.Path);
                string fileName = $"{height}px{extension}";

                var variation = GenerateVariation(imageDir, extension, original.Path, height, original.Width, original.Height);
                imageMetadata.Variations.Add(variation);

                await _imageRepository.UpdateImageAsync(imageMetadata);

                string variationPath = Path.Combine(imageDir, fileName);


                return variationPath;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException ioEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image variation.");
                throw new Exception("Failed to retrieve image variation.");
            }
        }

        /// <summary>
        /// Deletes an image and its associated files and metadata.
        /// </summary>
        /// <param name="imageId">The GUID of the image to delete.</param>
        public async Task DeleteImageAsync(Guid imageId)
        {
            try
            {
                var imageMetadata = await _imageRepository.GetImageByIdAsync(imageId);
                if (imageMetadata == null)
                {
                    throw new ArgumentException("Image not found.");
                }

                string imageDir = Path.GetDirectoryName(imageMetadata.Original.Path);


                if (Directory.Exists(imageDir))
                {
                    Directory.Delete(imageDir, true);
                }
                else // Directory doesn't exist, log a warning and proceed.
                {
                    _logger.LogWarning($"Image folder for image with ID {imageId} does not exist on disk.");
                }

                // Proceed with deleting the metadata from the repository
                await _imageRepository.DeleteImageAsync(imageId);

                _logger.LogInformation($"Image with ID {imageId} deleted successfully.");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "I/O error while deleting image files.");
                throw new InvalidOperationException("Failed to delete image files from disk.");
            }
            catch (UnauthorizedAccessException authEx)
            {
                _logger.LogError(authEx, "Access denied while deleting image files.");
                throw new InvalidOperationException("Access denied when deleting image files.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting image.");
                throw new Exception("Failed to delete image.");
            }
        }


        #region Helper methods
        private static ImageVariation GenerateVariation(string imageDir, string extension, string sourcePath, int height, int originalWidth, int originalHeight)
        {
            int width = CalculateWidth(originalWidth, originalHeight, height);
            string fileName = $"{height}px{extension}";
            string variationPath = Path.Combine(imageDir, fileName);

            File.Copy(sourcePath, variationPath);

            return new ImageVariation(height, width, variationPath);
        }

        public static int CalculateWidth(int originalWidth, int originalHeight, int targetHeight)
        {
            double aspectRatio = (double)originalWidth / originalHeight;
            return (int)Math.Round(targetHeight * aspectRatio);
        }

        private static async Task SaveToDisk(string originalPath, MemoryStream memoryStream)
        {
            memoryStream.Position = 0;
            using (var fileStream = new FileStream(originalPath, FileMode.Create))
            {
                await memoryStream.CopyToAsync(fileStream);
            }
        }


        #endregion
    }


}
