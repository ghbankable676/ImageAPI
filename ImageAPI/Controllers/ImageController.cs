using ImageAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageAPI.Controllers
{
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

        // Upload Image
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                string imageId = await _imageService.UploadImageAsync(file);
                return Ok(new { ImageId = imageId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Get Image Variation
        [HttpGet("{imageId:guid}/variation/{height:int}")]
        public async Task<IActionResult> GetImageVariation(Guid imageId, int height)
        {
            try
            {
                string imagePath = await _imageService.GetImageVariationAsync(imageId, height);
                return Ok(new { ImagePath = imagePath });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image variation.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Delete Image
        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            try
            {
                await _imageService.DeleteImageAsync(imageId);
                return Ok("Image deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
