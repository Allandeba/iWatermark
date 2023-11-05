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
        public IActionResult AddWatermark(Watermark watermark)
        {
            try
            {
                ValidateParameters();

                List<byte[]> processedImages = GetProcessedImages(watermark);

                MemoryStream zipFiles = GetZippedFile(processedImages);
                var zipFileName = "images.zip";

                // Todo: Verificar pois não ocorre a mensagem e a página não recarrega para demonstrar
                // TempData["SuccessMessage"] = "As imagens foram processadas com sucesso.";
                Response.Headers.Add("Content-Disposition", "attachment; filename=" + zipFileName);
                return File(zipFiles.ToArray(), "application/zip");
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao processar as imagens:";
                TempData["ErrorMessageException"] = e.Message;
                return View(nameof(Index), watermark);
            }
        }

        private void ValidateParameters()
        {
            if (!ModelState.IsValid)
            {
                throw new ArgumentException("Informações em vermelho são necessária.");
            }
        }

        private List<byte[]> GetProcessedImages(Watermark watermark)
        {
            using var logoImage = new MagickImage(watermark.Logomarca.OpenReadStream());
            logoImage.Format = MagickFormat.Svg;

            List<byte[]> processedImages = new List<byte[]>();

            Parallel.ForEach(watermark.Images, image =>
            {
                ProcessImage(image, logoImage, watermark.ParametrosWaterMark, processedImages);
            });

            return processedImages;
        }

        private void ProcessImage(IFormFile image, IMagickImage<ushort> logoImage, ParametrosWaterMark parametrosWaterMark, List<byte[]> processedImages)
        {
            using var imageToWatermark = new MagickImage(image.OpenReadStream());

            double maxLogoSize = GetMaxLogoSize(imageToWatermark, parametrosWaterMark);
            double iWatermarkScale = GetiWatermarkScale(maxLogoSize, logoImage);
            IMagickImage<ushort> watermarkClone = GetResizediWatermark(iWatermarkScale, logoImage);

            SetOpacityToiWatermark(watermarkClone, parametrosWaterMark.OpacidadeLogomarca);

            var watermarkPosition = GetWatermarkPosition(imageToWatermark, watermarkClone, parametrosWaterMark);
            ComposeiWatermarkToImage(imageToWatermark, watermarkClone, watermarkPosition.X, watermarkPosition.Y, CompositeOperator.Over);

            byte[] newImg = GetNewImage(imageToWatermark, 100, FormatoSaidaEnumClass.GetMagickFormat(parametrosWaterMark.FormatoSaida));
            processedImages.Add(newImg);
        }

        private double GetMaxLogoSize(MagickImage imageToWatermark, ParametrosWaterMark parametrosWaterMark)
        {
            return Math.Min(imageToWatermark.Width * parametrosWaterMark.GetProporcaoLogomarca(), imageToWatermark.Height * parametrosWaterMark.GetProporcaoLogomarca());
        }

        private double GetiWatermarkScale(double maxLogoSize, IMagickImage<ushort> logoImage)
        {
            return Math.Min(maxLogoSize / logoImage.Width, maxLogoSize / logoImage.Height);
        }

        private IMagickImage<ushort> GetResizediWatermark(double iWatermarkScale, IMagickImage<ushort> logoImage)
        {
            IMagickImage<ushort> watermarkClone = logoImage.Clone();

            int scaledWidth = (int)(watermarkClone.Width * iWatermarkScale);
            int scaledHeight = (int)(watermarkClone.Height * iWatermarkScale);
            watermarkClone.Resize(scaledWidth, scaledHeight);

            return watermarkClone;
        }

        private (int X, int Y) GetWatermarkPosition(IMagickImage<ushort> imageToWatermark, IMagickImage<ushort> watermarkClone, ParametrosWaterMark parametrosWaterMark)
        {
            int imgX = 0;
            int imgY = 0;
            
            switch (parametrosWaterMark.PosicaoLogomarca)
            {                
                case PosicaoLogomarcaEnum.Centro:
                    imgX = (imageToWatermark.Width - watermarkClone.Width) / 2;
                    imgY = (imageToWatermark.Height - watermarkClone.Height) / 2;
                    break;
                case PosicaoLogomarcaEnum.DireitaInferior:
                    imgX = imageToWatermark.Width - watermarkClone.Width;
                    imgY = imageToWatermark.Height - watermarkClone.Height;
                    break;
                case PosicaoLogomarcaEnum.EsquerdaInferior:
                    imgX = 0;
                    imgY = imageToWatermark.Height - watermarkClone.Height;
                    break;
                case PosicaoLogomarcaEnum.DireitaSuperior:
                    imgX = imageToWatermark.Width - watermarkClone.Width;
                    imgY = 0;
                    break;
                case PosicaoLogomarcaEnum.EsquerdaSuperior:
                    imgX = 0;
                    imgY = 0;
                    break;
                default:
                    throw new Exception("GetWatermarkPosition() not implemented!");                    
            }

            return (imgX, imgY);
        }

        private void SetOpacityToiWatermark(IMagickImage<ushort> watermarkClone, int opacity)
        {
            watermarkClone.Evaluate(Channels.Alpha, EvaluateOperator.Divide, opacity);
        }

        private void ComposeiWatermarkToImage(IMagickImage<ushort> imageToWatermark, IMagickImage<ushort> watermarkClone, int imgCenterX, int imgCenterY, CompositeOperator compositeOperator)
        {
            imageToWatermark.Composite(watermarkClone, imgCenterX, imgCenterY, compositeOperator);
        }

        private byte[] GetNewImage(IMagickImage<ushort> imageToWatermark, int quality, MagickFormat imgFormat)
        {
            using var outputImageStream = new MemoryStream();

            imageToWatermark.Quality = quality;
            imageToWatermark.Format = imgFormat;
            imageToWatermark.Write(outputImageStream);

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
