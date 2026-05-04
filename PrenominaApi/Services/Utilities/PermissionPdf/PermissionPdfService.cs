using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Layout.Borders;
using iText.Kernel.Colors;

namespace PrenominaApi.Services.Utilities.PermissionPdf
{
    public class PermissionPdfService
    {
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

            // Letter portrait: 612 x 792 pt. Cada copia ocupa media página verticalmente
            // (formato media carta lógico) usando un separador y una sola página física por copia.
            Document document = new Document(pdfDocument, PageSize.LETTER);
            document.SetMargins(36, 40, 36, 40);

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            string s(string? v) => string.IsNullOrEmpty(v) ? " " : v;

            RenderCopy(document, font, fontBold,
                s(company), s(employeeName), s(employeeCode), s(activity), s(department),
                s(date), s(permissionLabel), s(note), s(startDate), s(endDate), s(returnDate), s(totalDays),
                "Copia empleado");

            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            RenderCopy(document, font, fontBold,
                s(company), s(employeeName), s(employeeCode), s(activity), s(department),
                s(date), s(permissionLabel), s(note), s(startDate), s(endDate), s(returnDate), s(totalDays),
                "Copia empresa");

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
            // Encabezado de la empresa.
            document.Add(new Paragraph(company)
                .SetFont(fontBold)
                .SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(2));

            document.Add(new Paragraph("PERMISO PARA AUSENTARSE DEL TRABAJO")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(14));

            // Fecha alineada a la derecha.
            Paragraph dateParagraph = new Paragraph();
            dateParagraph.Add(new Text("Fecha: ").SetFont(font));
            dateParagraph.Add(new Text(date).SetFont(fontBold));
            dateParagraph.SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(6);
            document.Add(dateParagraph);

            // Información del empleado en una tabla 2x2.
            Table infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(12);

            infoTable.AddCell(InfoCell("Nombre: ", employeeName, font, fontBold));
            infoTable.AddCell(InfoCell("Puesto: ", activity, font, fontBold));
            infoTable.AddCell(InfoCell("Código: ", employeeCode, font, fontBold));
            infoTable.AddCell(InfoCell("Departamento: ", department, font, fontBold));
            document.Add(infoTable);

            document.Add(new Paragraph("Por medio del presente documento solicito el siguiente permiso:")
                .SetFont(font).SetFontSize(10).SetMarginBottom(4));

            document.Add(new Paragraph(permissionLabel)
                .SetFont(fontBold).SetFontSize(11).SetMarginBottom(6));

            Paragraph daysParagraph = new Paragraph();
            daysParagraph.Add(new Text("Días de ausencia que solicita: ").SetFont(fontBold));
            daysParagraph.Add(new Text(totalDays).SetFont(fontBold));
            daysParagraph.SetFontSize(10).SetMarginBottom(10);
            document.Add(daysParagraph);

            // Tabla de fechas: 3 columnas con porcentajes explícitos.
            Table datesTable = new Table(UnitValue.CreatePercentArray(new float[] { 33f, 34f, 33f }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(12);

            datesTable.AddCell(DateCell("Fecha Inicio: ", startDate, font, fontBold));
            datesTable.AddCell(DateCell("Fecha Termino: ", endDate, font, fontBold));
            datesTable.AddCell(DateCell("Fecha Regreso: ", returnDate, font, fontBold));
            document.Add(datesTable);

            document.Add(new Paragraph("MOTIVOS / OBSERVACIONES / RAZONES:")
                .SetFont(font).SetFontSize(10).SetMarginBottom(2));

            document.Add(new Paragraph(note)
                .SetFont(fontBold).SetFontSize(10).SetMarginBottom(36));

            // Tabla de firmas: 3 columnas iguales.
            Table signaturesTable = new Table(UnitValue.CreatePercentArray(new float[] { 33f, 34f, 33f }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            signaturesTable.AddCell(SignatureCell("Firma Empleado", font));
            signaturesTable.AddCell(SignatureCell("Firma jefe Depto", font));
            signaturesTable.AddCell(SignatureCell("Vo. Bo. Dpto R.R.H.H", font));
            document.Add(signaturesTable);

            document.Add(new Paragraph(copyLabel)
                .SetFont(font).SetFontSize(9).SetMarginTop(10).SetFontColor(ColorConstants.GRAY));
        }

        private static Cell InfoCell(string label, string value, PdfFont font, PdfFont fontBold)
        {
            Paragraph p = new Paragraph();
            p.Add(new Text(label).SetFont(font));
            p.Add(new Text(value).SetFont(fontBold));
            p.SetFontSize(10);

            return new Cell()
                .Add(p)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3);
        }

        private static Cell DateCell(string label, string value, PdfFont font, PdfFont fontBold)
        {
            Paragraph p = new Paragraph();
            p.Add(new Text(label).SetFont(font));
            p.Add(new Text(value).SetFont(fontBold));
            p.SetFontSize(10);

            return new Cell()
                .Add(p)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3);
        }

        private static Cell SignatureCell(string label, PdfFont font)
        {
            // Una sola Paragraph con salto de línea para evitar múltiples .Add() en la celda.
            Paragraph p = new Paragraph();
            p.Add(new Text("______________________________"));
            p.Add(new Text("\n"));
            p.Add(new Text(label));
            p.SetFont(font).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER);

            return new Cell()
                .Add(p)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(4);
        }
    }
}
