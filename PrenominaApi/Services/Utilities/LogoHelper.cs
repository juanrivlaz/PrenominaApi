using iText.IO.Image;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace PrenominaApi.Services.Utilities
{
    public static class LogoHelper
    {
        // Acepta data URLs (data:image/png;base64,...) o base64 puro.
        // Devuelve null si la entrada no es válida o no se puede decodificar.
        public static byte[]? DecodeDataUrl(string? dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;

            try
            {
                var commaIndex = dataUrl.IndexOf(',');
                var base64Part = commaIndex >= 0 ? dataUrl.Substring(commaIndex + 1) : dataUrl;
                base64Part = base64Part.Trim();
                if (string.IsNullOrEmpty(base64Part)) return null;

                return Convert.FromBase64String(base64Part);
            }
            catch
            {
                return null;
            }
        }

        // Construye un Image de iText listo para insertar; null si el logo no es decodificable
        // o no es un formato soportado por iText (PNG, JPG, GIF). SVG no es soportado directamente.
        public static Image? BuildPdfImage(string? dataUrl, float maxWidth = 80f, float maxHeight = 40f)
        {
            var bytes = DecodeDataUrl(dataUrl);
            if (bytes == null) return null;

            try
            {
                var imageData = ImageDataFactory.Create(bytes);
                var image = new Image(imageData);
                image.SetAutoScale(false);
                image.SetMaxHeight(maxHeight);
                image.SetMaxWidth(maxWidth);
                image.SetHorizontalAlignment(HorizontalAlignment.LEFT);
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
