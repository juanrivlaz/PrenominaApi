namespace PrenominaApi.Attributes
{
    /// <summary>
    /// Marca una entidad para que PrenominaDbContext genere registros automáticos
    /// en audit_log por cada Insert/Update/Delete. Opt-in.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AuditableAttribute : Attribute
    {
        /// <summary>Etiqueta humana de la entidad (ej: "Usuario", "Horario"). Se usa en Description.</summary>
        public string Label { get; }

        /// <summary>Código de sección para audit_log.SectionCode.</summary>
        public string SectionCode { get; }

        /// <summary>
        /// Nombres de propiedades (en orden) a intentar para identificar el registro
        /// en la descripción. Default: Name, FullName, Label, Email, Code, Key.
        /// </summary>
        public string[] IdentifierProperties { get; set; } = new[] { "Name", "FullName", "Label", "Email", "Code", "Key" };

        public AuditableAttribute(string label, string sectionCode)
        {
            Label = label;
            SectionCode = sectionCode;
        }
    }

    /// <summary>
    /// Etiqueta humana para una propiedad dentro del diff de audit (ej: "email", "rol").
    /// Si se omite, se usa el nombre de la propiedad.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AuditPropertyAttribute : Attribute
    {
        public string Label { get; }
        public bool Ignore { get; set; }
        public bool Sensitive { get; set; }

        public AuditPropertyAttribute(string label = "")
        {
            Label = label;
        }
    }
}
