namespace PrenominaApi.Models.Dto.Input
{
    public class EditAppearance
    {
        public string? PrimaryColor { get; set; }
        public string? SecondColor { get; set; }
        // Si viene null se ignora el campo. Si viene cadena vacía explícita se interpreta como "quitar logo".
        public string? Logo { get; set; }
    }

    // Wrapper para distinguir GET (sin payload).
    public class GetAppearance { }
}
