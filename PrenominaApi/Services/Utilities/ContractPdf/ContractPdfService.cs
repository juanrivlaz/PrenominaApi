using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using PrenominaApi.Models.Dto.Output;

namespace PrenominaApi.Services.Utilities.ContractPdf
{
    public class ContractPdfService
    {
        public byte[] Generate(IEnumerable<ContractsOutput> contracts, string companyName, string companyRFC) {
            using (MemoryStream stream = new MemoryStream()) {
                using (PdfWriter writer = new PdfWriter(stream)) {
                    writer.SetCloseStream(false);

                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        Document document = new Document(pdf);
                        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new ContractPdfHeader(document, companyName, companyRFC));
                        document.SetTopMargin(80);

                        // Agregar una tabla
                        var table = new Table(10).UseAllAvailableWidth();
                        table.AddHeaderCell(AddCellToHead("Código"));
                        table.AddHeaderCell(AddCellToHead("Nombre"));
                        table.AddHeaderCell(AddCellToHead("Departamento"));
                        table.AddHeaderCell(AddCellToHead("Actividad"));
                        table.AddHeaderCell(AddCellToHead("Antigüedad"));
                        table.AddHeaderCell(AddCellToHead("Inicio de Contrato"));
                        table.AddHeaderCell(AddCellToHead("Término de Contrato"));
                        table.AddHeaderCell(AddCellToHead("¿Generar Contrato?"));
                        table.AddHeaderCell(AddCellToHead("Días de Contrato"));
                        table.AddHeaderCell(AddCellToHead("Observación"));

                        int index = 0;
                        foreach (var item in contracts)
                        {
                            bool applyBgColor = index % 2 == 0;
                            table.AddCell(AddCellToTable(item.Codigo.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable($"{item.Name} {item.LastName} {item.MLastName}", applyBgColor));
                            table.AddCell(AddCellToTable(item.TenantName ?? "", applyBgColor));
                            table.AddCell(AddCellToTable(item.Activity ?? "", applyBgColor));
                            table.AddCell(AddCellToTable(item.SeniorityDate?.ToString("dd/MM/yyyy") ?? "", applyBgColor));
                            table.AddCell(AddCellToTable(item.StartDate?.ToString("dd/MM/yyyy") ?? "", applyBgColor));
                            table.AddCell(AddCellToTable(item.TerminationDate?.ToString("dd/MM/yyyy") ?? "", applyBgColor));
                            table.AddCell(AddCellToTable((bool)item.ApplyRehired! ? "SI" : "NO", applyBgColor));
                            table.AddCell(AddCellToTable(item.ContractDays.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable(item.Observation ?? "", applyBgColor));

                            index++;
                        }

                        document.Add(table);
                        document.Close();
                    }
                }

                return stream.ToArray();
            }
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
