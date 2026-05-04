namespace PrenominaApi.Models.Dto
{
    public class SysAppearance
    {
        public string PrimaryColor { get; set; } = "#5a6acf";
        public string SecondColor { get; set; } = "#2196f3";
        // Logo en formato data URL base64 (ej: "data:image/png;base64,iVBOR..."). Vacío = sin logo.
        public string Logo { get; set; } = string.Empty;
    }
}
