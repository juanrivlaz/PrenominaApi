using System.Text;
using iText.Html2pdf;

namespace PrenominaApi.Services.Utilities.PermissionPdf
{
    /// <summary>
    /// Renderiza una plantilla de documento (HTML con placeholders {{...}}) a PDF, reemplazando
    /// los valores reales. Las firmas se construyen a partir de la cadena de firmantes y se
    /// inyectan en el placeholder {{signatures}}.
    /// </summary>
    public class DocumentPdfRenderer
    {
        /// <summary>
        /// Reemplaza los placeholders del contenido y lo convierte a PDF.
        /// </summary>
        public byte[] Render(string htmlContent, IDictionary<string, string> values)
        {
            var html = ReplacePlaceholders(htmlContent, values);

            // Tipografía base igual a la del PDF anterior (Helvetica). Se aplica como default
            // a todo el documento; las plantillas pueden sobreescribir con su propio estilo.
            var styledHtml =
                "<style>" +
                "html, body, div, p, span, td, th, h1, h2, h3, h4 { font-family: Helvetica, Arial, sans-serif !important; }" +
                "body { font-size: 11pt; color: #000; }" +
                "</style>" + html;

            using var ms = new MemoryStream();
            var properties = new ConverterProperties();
            HtmlConverter.ConvertToPdf(styledHtml, ms, properties);
            return ms.ToArray();
        }

        private static string ReplacePlaceholders(string content, IDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            foreach (var kv in values)
            {
                content = content.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
            }

            return content;
        }

        /// <summary>
        /// Construye el bloque HTML de firmas: una celda para el empleado y una por cada nivel
        /// de la cadena (etiqueta del rol, y el nombre/fecha de quien firmó si ya está aprobado).
        /// </summary>
        public static string BuildSignaturesHtml(string employeeName, IEnumerable<SignatureBlock> blocks)
        {
            var cells = new List<string>
            {
                SignatureCell("Firma del empleado", employeeName, null)
            };

            cells.AddRange(blocks.Select(b => SignatureCell(b.RoleLabel, b.SignedByName, b.SignedAt)));

            // Se acomodan en filas de hasta 3 columnas.
            var sb = new StringBuilder();
            sb.Append("<table style=\"width:100%; border-collapse:collapse; margin-top:32px; text-align:center;\">");
            for (var i = 0; i < cells.Count; i += 3)
            {
                sb.Append("<tr>");
                for (var j = i; j < i + 3; j++)
                {
                    sb.Append(j < cells.Count ? cells[j] : "<td style=\"width:33%;\"></td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string SignatureCell(string caption, string? signedByName, string? signedAt)
        {
            var signer = string.IsNullOrWhiteSpace(signedByName)
                ? "&nbsp;"
                : System.Net.WebUtility.HtmlEncode(signedByName) + (string.IsNullOrWhiteSpace(signedAt) ? string.Empty : $"<br/><span style=\"font-size:9px; color:#666;\">{signedAt}</span>");

            return $@"<td style=""width:33%; padding:16px 8px; vertical-align:bottom;"">
                <div style=""font-size:10px; min-height:24px;"">{signer}</div>
                <div style=""border-top:1px solid #000; padding-top:6px; font-size:11px;"">{System.Net.WebUtility.HtmlEncode(caption)}</div>
            </td>";
        }

        public class SignatureBlock
        {
            public required string RoleLabel { get; set; }
            public string? SignedByName { get; set; }
            public string? SignedAt { get; set; }
        }
    }
}
