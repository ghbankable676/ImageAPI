using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageAPI.Models;

namespace ImageAPI.Repositories
{
    public interface IImageRepository
    {
        public Task InsertImageAsync(ImageMetadata metadata);
        public Task<ImageMetadata> GetImageByIdAsync(Guid id);
        public Task DeleteImageAsync(Guid imageId);
        public Task UpdateImageAsync(ImageMetadata metadata);
    }
}