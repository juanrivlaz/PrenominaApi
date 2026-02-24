using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Utilities.Attendance;
using System.Globalization;

namespace PrenominaApi.Services.Utilities.AttendancePdf
{
    public class AttendancePdfService
    {
        public byte[] Generate(
            IEnumerable<EmployeeAttendancesOutput> employeeAttendances,
            string companyName,
            string tenantName,
            string period,
            List<DateOnly> listDates,
            string rfcInfo,
            string typeNom
        )
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);
            using PdfDocument pdf = new PdfDocument(writer);

            List<OnlyIncidentCodeLabel> listIncidents = employeeAttendances
            .Where(e => e.Attendances != null && e.Attendances.Any()).SelectMany(e => (e.Attendances ?? Enumerable.Empty<AttendanceOutput>())
            .Where(a => a.IncidentCode != "--:--")
            .Select(a => new OnlyIncidentCodeLabel() { IncidentCode = a.IncidentCode, IncidentCodeLabel = a.IncidentCodeLabel }))
            .GroupBy(a => new OnlyIncidentCodeLabel() { IncidentCode = a.IncidentCode, IncidentCodeLabel = a.IncidentCodeLabel }).Select(g => g.First()).ToList();

            Document document = new Document(pdf, pageSize: PageSize.A4.Rotate());
            pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new AttendancePdfHeader(document, companyName, tenantName, typeNom, period, listIncidents, rfcInfo));
            document.SetTopMargin(101);

            foreach (var employee in employeeAttendances)
            {
                var table = new Table(listDates.Count + 1).UseAllAvailableWidth();
                table.AddHeaderCell(AddCellToHeadToAttendance(
                    $"Cod. {employee.Codigo} | {employee.Name} {employee.LastName} {employee.MLastName} | {employee.Activity}", listDates.Count, TextAlignment.LEFT, 5, true, true, true));
                table.AddHeaderCell(AddCellToHeadToAttendance("Observación", 1, TextAlignment.CENTER, 1, false, true, true, false));

                int indexDate = 1;
                foreach (var date in listDates)
                {
                    table.AddHeaderCell(AddCellToHeadDateToAttendance(date.ToString("dd/MM/yy"), 1, TextAlignment.CENTER, 1, indexDate == 1, false, indexDate == listDates.Count));
                    indexDate++;

                    //entry
                    var attendace = employee.Attendances?.FirstOrDefault(a => a.Date == date);
                    var checkEntry = attendace?.CheckEntry ?? attendace?.IncidentCode ?? "--:--";
                    var checkOut = attendace?.CheckOut ?? "--:--";
                    var divider = "/";

                    if (attendace?.CheckEntry == null && attendace?.IncidentCode != "--:--")
                    {
                        divider = "";
                    }

                    if (attendace?.CheckEntry == null && attendace?.IncidentCode != "--:--")
                    {
                        checkOut = "";
                    }

                    table.AddCell(AddCellToAttendace($"{checkEntry} {divider} {checkOut}", true));
                }
                //header observation
                table.AddHeaderCell(AddCellToHeadToAttendance("", 1, TextAlignment.CENTER, 1, false, false, true, false));
                table.AddCell(AddCellToAttendace("", true));

                document.Add(table);
            }

            document.Add(new Paragraph("\n").SetMarginTop(5));
            var tableSignature = new Table(2).UseAllAvailableWidth();
            tableSignature.AddHeaderCell(AddCellToSignature("______________________________"));
            tableSignature.AddHeaderCell(AddCellToSignature("______________________________"));
            tableSignature.AddCell(AddCellToSignature("JEFE DE DEPARTAMENTO"));
            tableSignature.AddCell(AddCellToSignature("DIRECCIÓN RECURSOS HUMANOS"));
            document.Add(tableSignature);

            document.Close();

            return memoryStream.ToArray();
        }

        private Cell AddCellToHeadToAttendance(string value, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(6).SetTextAlignment(textAlignment).SetFixedLeading(6)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToHeadDateToAttendance(string value, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(6).SetTextAlignment(textAlignment).SetMaxWidth(80)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToAttendace(string value, bool withBorderBottom = false)
        {
            Cell styledCell = new Cell().SetPadding(1);
            styledCell.Add(new Paragraph(value).SetFontSize(6).SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER).SetMaxWidth(80)).SetPadding(1)
            .SetBorderLeft(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
            .SetBorderRight(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
            .SetBorderTop(Border.NO_BORDER)
            .SetBorderBottom(withBorderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);

            return styledCell;
        }

        private Cell AddCellToSignature(string value)
        {
            Cell styledCell = new Cell().SetPadding(1);
            styledCell.Add(new Paragraph(value).SetFontSize(8).SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER).SetMinWidth(80)).SetPadding(1)
            .SetBorderLeft(Border.NO_BORDER)
            .SetBorderRight(Border.NO_BORDER)
            .SetBorderTop(Border.NO_BORDER)
            .SetBorderBottom(Border.NO_BORDER);

            return styledCell;
        }
    }
}
