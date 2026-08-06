using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;

namespace LibraryManagement.Services
{
    public class BarcodeService
    {
        private readonly IWebHostEnvironment _env;

        public BarcodeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GenerateBarcode(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return "";

            string folder = Path.Combine(
                _env.WebRootPath,
                "barcodes");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string safeFileName = isbn.Replace("/", "_")
                                      .Replace("\\", "_")
                                      .Replace(":", "_");

            string fileName = safeFileName + ".png";

            string filePath = Path.Combine(folder, fileName);

            if (!File.Exists(filePath))
            {
                var writer = new BarcodeWriter<SKBitmap>
                {
                    Format = BarcodeFormat.CODE_128,

                    Options = new EncodingOptions
                    {
                        Width = 500,
                        Height = 120,
                        Margin = 10,
                        PureBarcode = false
                    },

                    Renderer = new SKBitmapRenderer()
                };

                using SKBitmap bitmap = writer.Write(isbn);

                using SKImage image = SKImage.FromBitmap(bitmap);

                using SKData data = image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

                using FileStream stream =
                    File.OpenWrite(filePath);

                data.SaveTo(stream);
            }

            return "/barcodes/" + fileName;
        }
    }
}