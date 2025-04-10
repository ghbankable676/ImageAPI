using ImageAPI.Models;
using System.Text.Json;

namespace ImageAPI.Repositories
{
    /// <summary>
    /// A repository implementation for handling image metadata in an in-memory data store.
    /// </summary>
    public class ImageRepository : IImageRepository
    {
        private readonly string _dataFilePath;
        private Dictionary<Guid, ImageMetadata> _images;

        public ImageRepository(AppSettings settings)
        {
            _dataFilePath = Path.Combine(settings.ImageBasePath, "images.json");
            _images = LoadFromDisk();
        }

        private Dictionary<Guid, ImageMetadata> LoadFromDisk()
        {
            if (!File.Exists(_dataFilePath))
                return new Dictionary<Guid, ImageMetadata>();

            string json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<Dictionary<Guid, ImageMetadata>>(json)
                   ?? new Dictionary<Guid, ImageMetadata>();
        }

        private void SaveToDisk()
        {
            string json = JsonSerializer.Serialize(_images, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }

        public Task InsertImageAsync(ImageMetadata image)
        {
            _images[image.Id] = image;
            SaveToDisk();
            return Task.CompletedTask;
        }

        public Task<ImageMetadata> GetImageByIdAsync(Guid id)
        {
            _images.TryGetValue(id, out var image);
            return Task.FromResult(image);
        }

        public Task UpdateImageAsync(ImageMetadata updatedImage)
        {
            _images[updatedImage.Id] = updatedImage;
            SaveToDisk();
            return Task.CompletedTask;
        }

        public Task DeleteImageAsync(Guid id)
        {
            if (_images.Remove(id))
                SaveToDisk();
            return Task.CompletedTask;
        }
    }
}