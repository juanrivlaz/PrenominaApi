using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace PrenominaApi.Services.Utilities.ReportPdf
{
    /// <summary>
    /// Generador genérico de reportes tabulares en PDF (iText7). Recibe un título, un subtítulo
    /// con el contexto del reporte, los encabezados de columna y las filas ya formateadas.
    /// </summary>
    public class ReportPdfService
    {
        private static readonly DeviceRgb HeaderBackground = new DeviceRgb(33, 73, 125);
        private static readonly DeviceRgb BorderColor = new DeviceRgb(210, 210, 210);

        public byte[] Generate(
            string title,
            string subtitle,
            IReadOnlyList<string> headers,
            IReadOnlyList<string[]> rows)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);
            using PdfDocument pdf = new PdfDocument(writer);

            Document document = new Document(pdf, PageSize.A4.Rotate());
            document.SetMargins(24, 24, 24, 24);

            document.Add(new Paragraph(title)
                .SetFontSize(15)
                .SetBold()
                .SetMarginBottom(2));

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                document.Add(new Paragraph(subtitle)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginBottom(10));
            }

            var table = new Table(headers.Count).UseAllAvailableWidth();

            foreach (var header in headers)
            {
                table.AddHeaderCell(HeaderCell(header));
            }

            foreach (var row in rows)
            {
                foreach (var value in row)
                {
                    table.AddCell(BodyCell(value));
                }
            }

            document.Add(table);

            if (rows.Count == 0)
            {
                document.Add(new Paragraph("No se encontraron registros para los filtros seleccionados.")
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginTop(10));
            }

            document.Close();

            return memoryStream.ToArray();
        }

        private Cell HeaderCell(string value)
        {
            return new Cell()
                .Add(new Paragraph(value ?? string.Empty)
                    .SetFontSize(8)
                    .SetBold()
                    .SetFontColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.LEFT))
                .SetBackgroundColor(HeaderBackground)
                .SetPadding(4)
                .SetBorder(new SolidBorder(BorderColor, 0.5f));
        }

        private Cell BodyCell(string value)
        {
            return new Cell()
                .Add(new Paragraph(value ?? string.Empty)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.LEFT))
                .SetPadding(3)
                .SetBorder(new SolidBorder(BorderColor, 0.5f));
        }
    }
}
