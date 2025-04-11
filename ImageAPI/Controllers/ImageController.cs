using ImageAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageAPI.Controllers
{
    /// <summary>
    /// Handles HTTP requests related to image operations, such as uploading, retrieving, and deleting images.
    /// </summary>
    [ApiController]
    [Route("api/images")]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly ImageService _imageService;

        public ImageController(ILogger<ImageController> logger, ImageService imageService)
        {
            _logger = logger;
            _imageService = imageService;
        }

        /// <summary>
        /// Uploads an image to the server.
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <returns>A GUID representing the image</returns>
        // Upload Image
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                string imageId = await _imageService.UploadImageAsync(file);
                return Ok(new { ImageId = imageId });
            }
            catch (ArgumentException aeEx)
            {
                return BadRequest(new { Error = aeEx.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Failed to upload image." });
            }
        }

        /// <summary>
        /// Retrieves the original image by ID.
        /// </summary>
        /// <param name="imageId">The ID of the image.</param>
        /// <returns>Path to the original image</returns>
        [HttpGet("{imageId:guid}")]
        public async Task<IActionResult> GetImage(Guid imageId)
        {
            try
            {
                string imagePath = await _imageService.GetOriginalImageAsync(imageId);

                if (!System.IO.File.Exists(imagePath))
                    return NotFound(new { Error = "Image not found on disk." });

                string contentType = GetContentType(imagePath);
                return PhysicalFile(imagePath, contentType);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Failed to retrieve the original image." });
            }
        }


        /// <summary>
        /// Retrieves a specific image variation by ID and height.
        /// </summary>
        /// <param name="imageId">The ID of the image.</param>
        /// <param name="height">The height of the requested image variation.</param>
        /// <returns>Path to the image variation</returns>
        [HttpGet("{imageId:guid}/variation/{height:int}")]
        public async Task<IActionResult> GetImageVariation(Guid imageId, int height)
        {
            try
            {
                string imagePath = await _imageService.GetImageVariationAsync(imageId, height);

                if (!System.IO.File.Exists(imagePath))
                    return NotFound(new { Error = "Image variation not found on disk." });

                string contentType = GetContentType(imagePath);
                return PhysicalFile(imagePath, contentType);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Failed to retrieve image variation." });
            }
        }

        /// <summary>
        /// Deletes an image by its unique identifier.
        /// </summary>
        /// <param name="imageId">The unique identifier of the image to delete.</param>
        /// <returns>An ActionResult indicating the result of the deletion.</returns>
        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            try
            {
                await _imageService.DeleteImageAsync(imageId);
                return Ok("Image deleted successfully.");
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Failed to delete image." });
            }
        }

        #region Helper methods
        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
        #endregion
    }
}
