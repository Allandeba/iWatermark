using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ImageMagick;

namespace iWatermark.Models
{
    public class ParametrosWaterMark
    {
        [Display(Name = "Posição da logomarca")]
        public PosicaoLogomarcaEnum PosicaoLogomarca { get; set; }

        [Range(0.0, 100.0)]
        [Display(Name = "Proporção do tamanho da logomarca (%)")]
        public double ProporcaoLogomarca { get; set; }

        [Display(Name = "Opacidade da logomarca")]
        [Range(0, 100)]
        public int OpacidadeLogomarca { get; set; } = 2;

        [Display(Name = "Formato de arquivo de saída")]
        public FormatoSaidaEnumClass.FormatoSaidaEnum FormatoSaida { get; set; }

        public double GetProporcaoLogomarca()
        {
            return this.ProporcaoLogomarca / 100;
        }
    }

    public class Watermark
    {
        [Required]
        public required IFormFile Logomarca { get; set; }
        
        [Required]
        public required List<IFormFile> Images { get; set; }
        
        [Required]
        public required ParametrosWaterMark ParametrosWaterMark { get; set; }
    }
}