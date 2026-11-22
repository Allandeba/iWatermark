using iWatermark.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ImageMagick;
using System.IO.Compression;

namespace iWatermark.Controllers
{
    public class HomeController : Controller
    {
        private const int MaxCachedWatermarks = 8;
        private const ulong MaxCachedWatermarkPixels = 1_000_000;

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
            using var logoStream = watermark.Logomarca.OpenReadStream();
            using var logoImage = new MagickImage(logoStream);
            logoImage.Format = MagickFormat.Svg;

            var parameters = watermark.ParametrosWaterMark;
            var processedImages = new List<byte[]>();
            var preparedWatermarks = new Dictionary<(uint Width, uint Height), IMagickImage<ushort>>();
            ulong cachedPixels = 0;

            try
            {
                foreach (var image in watermark.Images)
                {
                    using var imageStream = image.OpenReadStream();
                    using var imageToWatermark = new MagickImage(imageStream);
                    var scale = GetiWatermarkScale(GetMaxLogoSize(imageToWatermark, parameters), logoImage);
                    var size = (Width: checked((uint)(logoImage.Width * scale)), Height: checked((uint)(logoImage.Height * scale)));

                    if (!preparedWatermarks.TryGetValue(size, out var preparedWatermark))
                    {
                        preparedWatermark = PrepareWatermark(logoImage, size, parameters.OpacidadeLogomarca);
                        var pixelCount = (ulong)size.Width * size.Height;
                        if (preparedWatermarks.Count >= MaxCachedWatermarks || pixelCount > MaxCachedWatermarkPixels - cachedPixels)
                        {
                            using (preparedWatermark)
                            {
                                processedImages.Add(ProcessImage(imageToWatermark, preparedWatermark, parameters));
                            }

                            continue;
                        }

                        preparedWatermarks.Add(size, preparedWatermark);
                        cachedPixels += pixelCount;
                    }

                    processedImages.Add(ProcessImage(imageToWatermark, preparedWatermark, parameters));
                }

                return processedImages;
            }
            finally
            {
                foreach (var preparedWatermark in preparedWatermarks.Values)
                {
                    preparedWatermark.Dispose();
                }
            }
        }

        private byte[] ProcessImage(MagickImage imageToWatermark, IMagickImage<ushort> preparedWatermark, ParametrosWaterMark parameters)
        {
            var position = GetWatermarkPosition(imageToWatermark, preparedWatermark, parameters);
            ComposeiWatermarkToImage(imageToWatermark, preparedWatermark, position.X, position.Y, CompositeOperator.Over);
            return GetNewImage(imageToWatermark, 100, FormatoSaidaEnumClass.GetMagickFormat(parameters.FormatoSaida));
        }

        private double GetMaxLogoSize(MagickImage imageToWatermark, ParametrosWaterMark parametrosWaterMark)
        {
            return Math.Min(imageToWatermark.Width * parametrosWaterMark.GetProporcaoLogomarca(), imageToWatermark.Height * parametrosWaterMark.GetProporcaoLogomarca());
        }

        private double GetiWatermarkScale(double maxLogoSize, IMagickImage<ushort> logoImage)
        {
            return Math.Min(maxLogoSize / logoImage.Width, maxLogoSize / logoImage.Height);
        }

        private IMagickImage<ushort> PrepareWatermark(IMagickImage<ushort> logoImage, (uint Width, uint Height) size, int opacity)
        {
            var preparedWatermark = logoImage.Clone();
            try
            {
                preparedWatermark.Resize(size.Width, size.Height);
                SetOpacityToiWatermark(preparedWatermark, opacity);
                return preparedWatermark;
            }
            catch
            {
                preparedWatermark.Dispose();
                throw;
            }
        }

        private (int X, int Y) GetWatermarkPosition(IMagickImage<ushort> imageToWatermark, IMagickImage<ushort> watermarkClone, ParametrosWaterMark parametrosWaterMark)
        {
            int imgX = 0;
            int imgY = 0;
            int offsetX = checked((int)imageToWatermark.Width - (int)watermarkClone.Width);
            int offsetY = checked((int)imageToWatermark.Height - (int)watermarkClone.Height);
            
            switch (parametrosWaterMark.PosicaoLogomarca)
            {                
                case PosicaoLogomarcaEnum.Centro:
                    imgX = offsetX / 2;
                    imgY = offsetY / 2;
                    break;
                case PosicaoLogomarcaEnum.DireitaInferior:
                    imgX = offsetX;
                    imgY = offsetY;
                    break;
                case PosicaoLogomarcaEnum.EsquerdaInferior:
                    imgX = 0;
                    imgY = offsetY;
                    break;
                case PosicaoLogomarcaEnum.DireitaSuperior:
                    imgX = offsetX;
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

        private byte[] GetNewImage(IMagickImage<ushort> imageToWatermark, uint quality, MagickFormat imgFormat)
        {
            using var outputImageStream = new MemoryStream();

            imageToWatermark.Quality = quality;
            imageToWatermark.Format = imgFormat;
            if (imgFormat == MagickFormat.Png)
            {
                // Keep the existing PNG filter and reduce lossless compression effort.
                imageToWatermark.Settings.SetDefine(MagickFormat.Png, "compression-level", 3);
            }

            imageToWatermark.Write(outputImageStream);

            return outputImageStream.ToArray();
        }

        private MemoryStream GetZippedFile(List<byte[]> images)
        {
            using var zipMemoryStream = new MemoryStream();

            using var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create);

            for (int i = 0; i < images.Count; i++)
            {
                var entry = archive.CreateEntry($"image{i}.png", CompressionLevel.NoCompression);
                using var entryStream = entry.Open();
                entryStream.Write(images[i], 0, images[i].Length);
            }
            
            return zipMemoryStream;
        }
    }
}
