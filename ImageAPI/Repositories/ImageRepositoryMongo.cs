using ImageAPI.Models;
using MongoDB.Driver;

namespace ImageAPI.Repositories
{
    /// <summary>
    /// A repository implementation for handling image metadata in a MongoDB database.
    /// </summary>
    public class ImageRepositoryMongo : IImageRepository
    {
        private readonly IMongoCollection<ImageMetadata> _imageCollection;
        private readonly IMongoDatabase _db;

        public ImageRepositoryMongo(IMongoClient mongoClient)
        {
            _db = mongoClient.GetDatabase("ImageDB");
            _imageCollection = _db.GetCollection<ImageMetadata>("Images");
        }

        public async Task InsertImageAsync(ImageMetadata imageMetadata)
        {
            await _imageCollection.InsertOneAsync(imageMetadata);
        }

        public async Task<ImageMetadata> GetImageByIdAsync(Guid imageId)
        {
            return await _imageCollection.Find(x => x.Id == imageId).FirstOrDefaultAsync();
        }

        public async Task DeleteImageAsync(Guid imageId)
        {
            await _imageCollection.DeleteOneAsync(x => x.Id == imageId);
        }

        public async Task UpdateImageAsync(ImageMetadata imageMetadata)
        {
            var filter = Builders<ImageMetadata>.Filter.Eq(x => x.Id, imageMetadata.Id);
            var update = Builders<ImageMetadata>.Update.Set(x => x.Variations, imageMetadata.Variations);

            await _imageCollection.UpdateOneAsync(filter, update);
        }
    }
}
