using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageCompressionController : ControllerBase
    {
        private readonly ILogger<ImageCompressionController> _logger;
        private const int MaxFileSize = 4 * 1024 * 1024; // 4MB

        public ImageCompressionController(ILogger<ImageCompressionController> logger)
        {
            _logger = logger;
        }

        [HttpPost("compress")]
        public async Task<IActionResult> CompressImage(IFormFile imageFile)
        {
            // Check if the uploaded file is an image
            if (imageFile == null || !IsSupportedImageContentType(imageFile.ContentType))
            {
                return BadRequest("Invalid image file.");
            }

            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);

            // Check if the image size exceeds 4MB
            if (ms.Length <= MaxFileSize)
            {
                // If the image is within the limit, return it as-is
                return File(ms.ToArray(), "image/jpeg");
            }

            // Compress the image to fit the 4MB limit
            var compressedImage = await CompressToSizeAsync(ms, MaxFileSize);

            if (compressedImage == null)
            {
                return StatusCode(500, "Failed to compress the image.");
            }

            return File(compressedImage.ToArray(), "image/jpeg");
        }

        private bool IsSupportedImageContentType(string contentType)
        {
            return contentType.StartsWith("image/jpeg");
        }

        private async Task<MemoryStream> CompressToSizeAsync(Stream inputStream, int targetSize)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
            using var input = await Image.LoadAsync(inputStream);
            using var outputStream = new MemoryStream();
            await input.SaveAsJpegAsync(outputStream, new JpegEncoder() { Quality = 80});

            if (outputStream.Length <= targetSize)
            {
                return outputStream;
            }

            return null;
        }
    }
}
