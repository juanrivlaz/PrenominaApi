using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using PrenominaApi.Models.Dto.Output.Attendance;

namespace PrenominaApi.Services.Utilities.AdditionalPayPdf
{
    public class AdditionalPayPdfService
    {
        public byte[] Generate(IEnumerable<AdditionalPay> additionalPays, string companyName, string companyRFC)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);

            writer.SetCloseStream(false);
            using PdfDocument pdfDocument = new PdfDocument(writer);
            Document document = new Document(pdfDocument);
            pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, new AdditionalPayPdfHeader(document, companyName, companyRFC));
            document.SetTopMargin(80);

            // Agregar una tabla con los datos de AdditionalPay
            var table = new Table(8).UseAllAvailableWidth();
            table.AddHeaderCell(AddCellToHead("Empleado"));
            table.AddHeaderCell(AddCellToHead("Fecha"));
            table.AddHeaderCell(AddCellToHead("Código de Incidencia"));
            table.AddHeaderCell(AddCellToHead("Columna"));
            table.AddHeaderCell(AddCellToHead("Valor Base"));
            table.AddHeaderCell(AddCellToHead("Operador"));
            table.AddHeaderCell(AddCellToHead("Valor de Operación"));
            table.AddHeaderCell(AddCellToHead("Total"));

            int index = 0;

            foreach (var pay in additionalPays)
            {
                bool bgColor = index % 2 == 0;
                table.AddCell(AddCellToTable(pay.EmployeeName, bgColor));
                table.AddCell(AddCellToTable(pay.Date.ToString("dd/MM/yyyy"), bgColor));
                table.AddCell(AddCellToTable(pay.IncidentCode, bgColor));
                table.AddCell(AddCellToTable(pay.Column, bgColor));
                table.AddCell(AddCellToTable(pay.BaseValue.ToString("C2"), bgColor));
                table.AddCell(AddCellToTable(pay.OperatorText, bgColor));
                table.AddCell(AddCellToTable(pay.OperationValue.ToString(), bgColor));
                table.AddCell(AddCellToTable(pay.Total.ToString("C2"), bgColor));
                index++;
            }

            document.Add(table);
            document.Close();

            return memoryStream.ToArray();
        }

        private Cell AddCellToHead(string value)
        {
            return new Cell().Add(
                new Paragraph(value).SetFontSize(8)
            ).SetPadding(6)
            .SetBackgroundColor(new DeviceRgb(236, 240, 243))
            .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f));
        }

        private Cell AddCellToTable(string value, bool bgColor)
        {
            return new Cell().Add(
                new Paragraph(value).SetFontSize(7)
            ).SetPadding(6)
            .SetBackgroundColor(bgColor ? new DeviceRgb(249, 251, 252) : new DeviceRgb(255, 255, 255))
            .SetBorderLeft(Border.NO_BORDER)
            .SetBorderRight(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f));
        }
    }
}
