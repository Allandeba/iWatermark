using System.ComponentModel.DataAnnotations;
using ImageMagick;

namespace iWatermark.Models
{
    public enum PosicaoLogomarcaEnum
    {
        Centro,
        
        [Display(Name = "Direita inferior")]
        DireitaInferior,
        
        [Display(Name = "Esquerda inferior")]
        EsquerdaInferior,
        
        [Display(Name = "Direita superior")]
        DireitaSuperior,
        
        [Display(Name = "Esquerda superior")]
        EsquerdaSuperior
    }

    public static class FormatoSaidaEnumClass
    {
        public enum FormatoSaidaEnum
        {
            Png
        }

        public static MagickFormat GetMagickFormat(FormatoSaidaEnum formatoSaida)
        {
            switch (formatoSaida)
            {
                case FormatoSaidaEnum.Png: 
                    return MagickFormat.Png;
                default: 
                    return MagickFormat.Unknown;
            }
        }
    }
}