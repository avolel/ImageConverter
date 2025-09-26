using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
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
                return File(ms.ToArray(), "image/png");
            }

            // Compress the image to fit the 4MB limit
            var compressedImage = await CompressToSizeAsync(ms, MaxFileSize);

            if (compressedImage == null)
            {
                return StatusCode(500, "Failed to compress the image.");
            }

            return File(compressedImage.ToArray(), "image/png");
        }

        private bool IsSupportedImageContentType(string contentType)
        {
            return contentType.StartsWith("image/");
        }

        private async Task<MemoryStream> CompressToSizeAsync(Stream inputStream, int targetSize)
        {
            using var input = Image.Load(inputStream);
            //var quality = 80; // Initial quality setting

            //while (quality > 10)
            //{
                using var outputStream = new MemoryStream();
                await input.SaveAsPngAsync(outputStream, new PngEncoder());

                if (outputStream.Length <= targetSize)
                {
                    return outputStream;
                }

                //quality -= 10; // Reduce quality and try again
            //}

            return null;
        }
    }
}
