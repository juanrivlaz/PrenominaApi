using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Layout.Borders;

namespace PrenominaApi.Services.Utilities.PermissionPdf
{
    public class PermissionPdfService
    {
        // Half-letter (media carta): 8.5" x 5.5" = 612 x 396 pt
        private static readonly PageSize HalfLetter = new PageSize(612f, 396f);

        public byte[] Generate(
            string company,
            string employeeName,
            string employeeCode,
            string activity,
            string department,
            string date,
            string permissionLabel,
            string note,
            string startDate,
            string endDate,
            string returnDate,
            string totalDays)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);
            using PdfDocument pdfDocument = new PdfDocument(writer);

            Document document = new Document(pdfDocument, HalfLetter);
            document.SetMargins(24, 28, 24, 28);

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Página 1: copia empleado
            RenderCopy(document, font, fontBold, company, employeeName, employeeCode, activity, department,
                date, permissionLabel, note, startDate, endDate, returnDate, totalDays, "Copia empleado");

            // Salto de página y copia empresa
            document.Add(new AreaBreak());
            RenderCopy(document, font, fontBold, company, employeeName, employeeCode, activity, department,
                date, permissionLabel, note, startDate, endDate, returnDate, totalDays, "Copia empresa");

            document.Close();
            return memoryStream.ToArray();
        }

        private static void RenderCopy(
            Document document,
            PdfFont font,
            PdfFont fontBold,
            string company,
            string employeeName,
            string employeeCode,
            string activity,
            string department,
            string date,
            string permissionLabel,
            string note,
            string startDate,
            string endDate,
            string returnDate,
            string totalDays,
            string copyLabel)
        {
            document.Add(new Paragraph(company)
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(0)
                .SetFixedLeading(12));

            document.Add(new Paragraph("PERMISO PARA AUSENTARSE DEL TRABAJO")
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10)
                .SetFixedLeading(11));

            // Fecha alineada a la derecha
            document.Add(new Paragraph()
                .Add(new Text("Fecha: ").SetFont(font))
                .Add(new Text(date).SetFont(fontBold))
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginBottom(6)
                .SetFixedLeading(10));

            var infoTable = new Table(new float[] { 1, 1 })
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            infoTable.AddCell(InfoCell("Nombre: ", employeeName, font, fontBold));
            infoTable.AddCell(InfoCell("Puesto: ", activity, font, fontBold));
            infoTable.AddCell(InfoCell("Código: ", employeeCode, font, fontBold));
            infoTable.AddCell(InfoCell("Departamento: ", department, font, fontBold));

            document.Add(infoTable);

            document.Add(new Paragraph("Por medio del presente documento solicito el siguiente permiso:")
                .SetFont(font)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetMarginTop(10)
                .SetMarginBottom(4)
                .SetFixedLeading(10));

            document.Add(new Paragraph(permissionLabel)
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetMarginBottom(4)
                .SetFixedLeading(11));

            document.Add(new Paragraph()
                .Add(new Text("Días de ausencia que solicita: ").SetFont(fontBold))
                .Add(new Text(totalDays).SetFont(fontBold))
                .SetFontSize(9)
                .SetMarginBottom(8)
                .SetFixedLeading(10));

            // Fechas en una sola fila estilo tabla
            var datesTable = new Table(3).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
            datesTable.AddCell(DateCell("Fecha Inicio: ", startDate, font, fontBold));
            datesTable.AddCell(DateCell("Fecha Termino: ", endDate, font, fontBold));
            datesTable.AddCell(DateCell("Fecha Regreso: ", returnDate, font, fontBold));
            document.Add(datesTable);

            document.Add(new Paragraph("MOTIVOS / OBSERVACIONES / RAZONES:")
                .SetFont(font)
                .SetFontSize(9)
                .SetMarginTop(8)
                .SetMarginBottom(2)
                .SetFixedLeading(10));

            document.Add(new Paragraph(note)
                .SetFont(fontBold)
                .SetFontSize(9)
                .SetMarginBottom(20)
                .SetFixedLeading(10));

            // Firmas
            var signaturesTable = new Table(3).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
            signaturesTable.AddCell(SignatureLine("Firma Empleado", font));
            signaturesTable.AddCell(SignatureLine("Firma jefe Depto", font));
            signaturesTable.AddCell(SignatureLine("Vo. Bo. Dpto R.R.H.H", font));
            document.Add(signaturesTable);

            document.Add(new Paragraph(copyLabel)
                .SetFont(font)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));
        }

        private static Cell InfoCell(string label, string value, PdfFont font, PdfFont fontBold)
        {
            return new Cell()
                .Add(new Paragraph()
                    .Add(new Text(label).SetFont(font))
                    .Add(new Text(value ?? string.Empty).SetFont(fontBold))
                    .SetFontSize(9)
                    .SetFixedLeading(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2);
        }

        private static Cell DateCell(string label, string value, PdfFont font, PdfFont fontBold)
        {
            return new Cell()
                .Add(new Paragraph()
                    .Add(new Text(label).SetFont(font))
                    .Add(new Text(value ?? string.Empty).SetFont(fontBold))
                    .SetFontSize(9)
                    .SetFixedLeading(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2);
        }

        private static Cell SignatureLine(string label, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph("______________________________")
                    .SetFont(font).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER).SetFixedLeading(10))
                .Add(new Paragraph(label)
                    .SetFont(font).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER).SetFixedLeading(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2);
        }
    }
}
