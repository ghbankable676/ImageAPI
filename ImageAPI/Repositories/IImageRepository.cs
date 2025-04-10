using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageAPI.Models;

namespace ImageAPI.Repositories
{
    /// <summary>
    /// Defines the interface for repository operations related to images and their metadata.
    /// </summary>
    public interface IImageRepository
    {
        public Task InsertImageAsync(ImageMetadata metadata);
        public Task<ImageMetadata> GetImageByIdAsync(Guid id);
        public Task DeleteImageAsync(Guid imageId);
        public Task UpdateImageAsync(ImageMetadata metadata);
    }
}