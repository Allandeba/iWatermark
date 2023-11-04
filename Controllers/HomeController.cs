using iWatermark.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ImageMagick;
using System.IO.Compression;

namespace iWatermark.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult AddWatermark(IFormFile logo, List<IFormFile> images)
        {
            try
            {
                ValidateParameters(logo, images);

                List<byte[]> processedImages = GetProcessedImages(logo, images);

                MemoryStream zipFiles = GetZippedFile(processedImages);
                var zipFileName = "images.zip";

                // Todo: Verificar pois não ocorre a mensagem e a página não recarrega para demonstrar
                // TempData["SuccessMessage"] = "As imagens foram processadas com sucesso.";
                Response.Headers.Add("Content-Disposition", "attachment; filename=" + zipFileName);
                return File(zipFiles.ToArray(), "application/zip");
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao processar as imagens:\n" + e.Message;
                return View(nameof(Index));
            }
        }

        private void ValidateParameters(IFormFile logo, List<IFormFile> images)
        {
            if (logo == null || images == null || images.Count == 0)
            {
                throw new ArgumentException("Ambas as imagens devem ser fornecidas.");
            }
        }

        private List<byte[]> GetProcessedImages(IFormFile logo, List<IFormFile> images)
        {
            using var logoImage = new MagickImage(logo.OpenReadStream());
            logoImage.Format = MagickFormat.Svg;

            List<byte[]> processedImages = new List<byte[]>();

            Parallel.ForEach(images, image =>
            {
                ProcessImage(image, logoImage, processedImages);
            });

            return processedImages;
        }

        private void ProcessImage(IFormFile image, IMagickImage<ushort> logoImage, List<byte[]> processedImages)
        {
            using var imageToiWatermark = new MagickImage(image.OpenReadStream());

            double maxLogoSize = GetMaxLogoSize(imageToiWatermark);
            double iWatermarkScale = GetiWatermarkScale(maxLogoSize, logoImage);
            IMagickImage<ushort> iWatermarkClone = GetResizediWatermark(iWatermarkScale, logoImage);

            SetOpacityToiWatermark(iWatermarkClone, 2);

            int imgCenterX = (imageToiWatermark.Width - iWatermarkClone.Width) / 2;
            int imgCenterY = (imageToiWatermark.Height - iWatermarkClone.Height) / 2;
            ComposeiWatermarkToImage(imageToiWatermark, iWatermarkClone, imgCenterX, imgCenterY, CompositeOperator.Over);

            byte[] newImg = GetNewImage(imageToiWatermark, 100, MagickFormat.Png);
            processedImages.Add(newImg);
        }

        private double GetMaxLogoSize(MagickImage imageToiWatermark)
        {
            return Math.Min(imageToiWatermark.Width * 0.5, imageToiWatermark.Height * 0.5);
        }

        private double GetiWatermarkScale(double maxLogoSize, IMagickImage<ushort> logoImage)
        {
            return Math.Min(maxLogoSize / logoImage.Width, maxLogoSize / logoImage.Height);
        }

        private IMagickImage<ushort> GetResizediWatermark(double iWatermarkScale, IMagickImage<ushort> logoImage)
        {
            IMagickImage<ushort> iWatermarkClone = logoImage.Clone();

            int scaledWidth = (int)(iWatermarkClone.Width * iWatermarkScale);
            int scaledHeight = (int)(iWatermarkClone.Height * iWatermarkScale);
            iWatermarkClone.Resize(scaledWidth, scaledHeight);

            return iWatermarkClone;
        }

        private void SetOpacityToiWatermark(IMagickImage<ushort> iWatermarkClone, int opacity)
        {
            iWatermarkClone.Evaluate(Channels.Alpha, EvaluateOperator.Divide, opacity);
        }

        private void ComposeiWatermarkToImage(IMagickImage<ushort> imageToiWatermark, IMagickImage<ushort> iWatermarkClone, int imgCenterX, int imgCenterY, CompositeOperator compositeOperator)
        {
            imageToiWatermark.Composite(iWatermarkClone, imgCenterX, imgCenterY, compositeOperator);
        }

        private byte[] GetNewImage(IMagickImage<ushort> imageToiWatermark, int quality, MagickFormat imgFormat)
        {
            using var outputImageStream = new MemoryStream();

            imageToiWatermark.Quality = quality;
            imageToiWatermark.Format = imgFormat;
            imageToiWatermark.Write(outputImageStream);

            return outputImageStream.ToArray();
        }

        private MemoryStream GetZippedFile(List<byte[]> images)
        {
            using var zipMemoryStream = new MemoryStream();

            using var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create);

            for (int i = 0; i < images.Count; i++)
            {
                var entry = archive.CreateEntry($"image{i}.png");
                using var entryStream = entry.Open();
                entryStream.Write(images[i], 0, images[i].Length);
            }
            
            return zipMemoryStream;
        }
    }
}
