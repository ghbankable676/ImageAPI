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

namespace ImageAPITests.Services
{
    public class ImageServiceTests
    {
        private readonly Mock<IImageRepository> _mockRepo = new();
        private readonly Mock<ILogger<ImageService>> _mockLogger = new();
        private readonly ImageService _imageService;

        public ImageServiceTests()
        {
            var appSettings = new AppSettings
            {
                ImageBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestImages")
            };

            _imageService = new ImageService(_mockLogger.Object, _mockRepo.Object, appSettings);
        }

        [Fact]
        public async Task UploadImageAsync_ShouldStoreImageAndVariations()
        {
            // Arrange
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

            // Act
            var result = await _imageService.UploadImageAsync(formFile);

            // Assert
            result.Should().NotBeNullOrEmpty();
            _mockRepo.Verify(r => r.InsertImageAsync(It.IsAny<ImageMetadata>()), Times.Once);

            capturedMetadata.Should().NotBeNull();
            capturedMetadata.Original.Should().NotBeNull();
            capturedMetadata.Variations.Should().NotBeEmpty();
            capturedMetadata.Original.Height.Should().Be(1080);  // original image height
            capturedMetadata.Original.Width.Should().Be(1920);  // original image width

            // Get the file extension of the original file
            var originalExtension = Path.GetExtension(filePath).ToLower();  // e.g., ".png"

            // Assert variations are stored
            capturedMetadata.Variations.Should().Contain(v => v.Height == 160);
            capturedMetadata.Variations.Should().Contain(v => v.Height == 900);
            capturedMetadata.Variations.Should().Contain(v => v.Height == 768);
            capturedMetadata.Variations.Should().Contain(v => v.Height == 720);
            capturedMetadata.Variations.Should().Contain(v => v.Height == 480);
            capturedMetadata.Variations.Should().Contain(v => v.Height == 360);



            // Assert the thumbnail exists (160px)
            var thumbnail = capturedMetadata.Variations.FirstOrDefault(v => v.Height == 160);
            thumbnail.Should().NotBeNull();
            thumbnail.Path.Should().EndWith($"160px{originalExtension}");

            string imageBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestImages");
            // Assert a file exists for each variation
            string variationPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"900px{originalExtension}");
            File.Exists(variationPath).Should().BeTrue();

            variationPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"768px{originalExtension}");
            File.Exists(variationPath).Should().BeTrue();

            variationPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"720px{originalExtension}");
            File.Exists(variationPath).Should().BeTrue();

            variationPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"480px{originalExtension}");
            File.Exists(variationPath).Should().BeTrue();

            variationPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"360px{originalExtension}");
            File.Exists(variationPath).Should().BeTrue();

            // Assert a copy of the original file exists for the thumbnail (160px)
            var thumbnailPath = Path.Combine(imageBasePath, capturedMetadata.Id.ToString(), $"160px{originalExtension}");
            File.Exists(thumbnailPath).Should().BeTrue();
        }
    }
}