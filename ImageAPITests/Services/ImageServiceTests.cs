using Xunit;
using Moq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ImageAPI.Services;
using ImageAPI.Models;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using ImageAPI.Repositories;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageAPITests.Services
{
    public class ImageServiceTests
    {
        private readonly Mock<IImageRepository> _mockRepo = new();
        private readonly Mock<ILogger<ImageService>> _mockLogger = new();
        private readonly ImageService _imageService;
        private readonly string _basePath;

        public ImageServiceTests()
        {
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "TestImages");
            var appSettings = new AppSettings { ImageBasePath = _basePath };
            _imageService = new ImageService(_mockLogger.Object, _mockRepo.Object, appSettings);

            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        #region UploadImageAsync
        [Fact]
        public async Task UploadImageAsync_ShouldStoreImageAndVariations()
        {
            var (id, metadata) = await SimulateImageUpload();

            metadata.Should().NotBeNull();
            metadata.Original.Should().NotBeNull();
            metadata.Original.Height.Should().Be(1080);
            metadata.Original.Width.Should().Be(1920);

            metadata.Variations.Should().Contain(v => v.Height == 160);
            metadata.Variations.Should().Contain(v => v.Height == 900);
            metadata.Variations.Should().Contain(v => v.Height == 768);
            metadata.Variations.Should().Contain(v => v.Height == 720);
            metadata.Variations.Should().Contain(v => v.Height == 480);
            metadata.Variations.Should().Contain(v => v.Height == 360);

            foreach (var variation in metadata.Variations)
            {
                File.Exists(variation.Path).Should().BeTrue();
            }
        }
        #endregion

        #region GetOriginalImageAsync
        [Fact]
        public async Task GetOriginalImageAsync_ShouldReturnImage_WhenImageExists()
        {
            // Arrange
            var (id, metadata) = await SimulateImageUpload();
            _mockRepo.Setup(r => r.GetImageByIdAsync(id)).ReturnsAsync(metadata);

            // Act
            var result = await _imageService.GetOriginalImageAsync(id);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var fileInfo = new FileInfo(result);
            fileInfo.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task GetOriginalImageAsync_ShouldThrowArgumentException_WhenImageDoesNotExist()
        {
            var id = Guid.NewGuid(); // Use a random GUID to simulate a non-existing image
            _mockRepo.Setup(r => r.GetImageByIdAsync(id)).ReturnsAsync((ImageMetadata)null);
             
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _imageService.GetOriginalImageAsync(id));
        }
        #endregion

        #region GetImageVariationAsync
        [Theory]
        [InlineData(1080)]
        [InlineData(720)]
        [InlineData(700)] // Not in aspect_ratios.json
        [InlineData(1440)] // Higher than original (should throw error)
        [InlineData(160)] // Thumbnail
        public async Task GetImageVariationAsync_ShouldReturnCorrectVariation_AndPreserveAspectRatio(int requestedHeight)
        {
            // Arrange
            var (id, metadata) = await SimulateImageUpload();
            _mockRepo.Setup(r => r.GetImageByIdAsync(id)).ReturnsAsync(metadata);

            var originalRatio = (double)metadata.Original.Width / metadata.Original.Height;

            if (requestedHeight > metadata.Original.Height)
            {
                // If the requested height is greater than the original, expect an exception
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await _imageService.GetImageVariationAsync(id, requestedHeight));
            }
            else
            {
                // Act
                var result = await _imageService.GetImageVariationAsync(id, requestedHeight);

                // Assert
                result.Should().NotBeNullOrEmpty();
                var returnedFilePath = result;
                var fileInfo = new FileInfo(returnedFilePath);
                fileInfo.Exists.Should().BeTrue();

                var variation = metadata.Variations.FirstOrDefault(v => v.Path == returnedFilePath)
                             ?? (metadata.Original.Path == returnedFilePath ? metadata.Original : null);

                variation.Should().NotBeNull();

                originalRatio = Math.Round(originalRatio, 2);
                var actualRatio = Math.Round((double)variation.Width / variation.Height, 2);

                actualRatio.Should().Be(originalRatio);
            }
        }
        #endregion

        #region DeleteImageAsync
        [Fact]
        public async Task DeleteImageAsync_ShouldDeleteFromMetadataAndDisk_WhenImageExists()
        {
            var (id, metadata) = await SimulateImageUpload();
            _mockRepo.Setup(r => r.GetImageByIdAsync(id)).ReturnsAsync(metadata);
            _mockRepo.Setup(r => r.DeleteImageAsync(id)).Returns(Task.CompletedTask);

            await _imageService.DeleteImageAsync(id);

            _mockRepo.Verify(r => r.DeleteImageAsync(id), Times.Once);
            Directory.Exists(Path.Combine(_basePath, id.ToString())).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteImageAsync_ShouldDeleteMetadata_WhenFolderDoesNotExist()
        {
            var guid = Guid.NewGuid();
            var metadata = new ImageMetadata
            {
                Id = guid,
                Original = new ImageVariation(1080, 1920, Path.Combine(_basePath, guid.ToString(), "original.png")),
                Variations = new List<ImageVariation>()
            };

            _mockRepo.Setup(r => r.GetImageByIdAsync(guid)).ReturnsAsync(metadata);
            _mockRepo.Setup(r => r.DeleteImageAsync(guid)).Returns(Task.CompletedTask);

            await _imageService.DeleteImageAsync(guid);

            _mockRepo.Verify(r => r.DeleteImageAsync(guid), Times.Once);
        }
        #endregion

        #region Helper methods
        private async Task<(Guid id, ImageMetadata metadata)> SimulateImageUpload()
        {
            var filePath = Path.Combine("TestFiles", "test_1920x1080_169ratio.png");
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", "test_1920x1080_169ratio.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            ImageMetadata capturedMetadata = null;
            _mockRepo.Setup(r => r.InsertImageAsync(It.IsAny<ImageMetadata>()))
                     .Callback<ImageMetadata>(meta => capturedMetadata = meta)
                     .Returns(Task.CompletedTask);

            await _imageService.UploadImageAsync(formFile);

            return (capturedMetadata.Id, capturedMetadata);
        }
        #endregion
    }
}