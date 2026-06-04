using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PrenominaApi.Models.Dto;
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
            string typeNom,
            SysConfigReports? sysConfig = null,
            string? logoDataUrl = null
        )
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);
            using PdfDocument pdf = new PdfDocument(writer);

            sysConfig ??= new SysConfigReports();
            int fontSize = sysConfig.ConfigAttendanceReport?.CompactFontSize > 0 ? sysConfig.ConfigAttendanceReport.CompactFontSize : 6;
            bool showDayInitial = sysConfig.ConfigAttendanceReport?.ShowDayInitial ?? false;
            NameOrder nameOrder = sysConfig.ConfigNameFormat?.Order ?? NameOrder.FirstNameFirst;
            var signatures = (sysConfig.ConfigSignatures?.Signatures ?? new List<SignatureItem>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Position))
                .Take(4)
                .ToList();

            // Lista de incidencias para el encabezado: todas las que aparecen en los registros,
            // deduplicadas por código. Se ignoran códigos vacíos o placeholder (N/A, "--:--").
            List<OnlyIncidentCodeLabel> listIncidents = employeeAttendances
            .Where(e => e.Attendances != null && e.Attendances.Any())
            .SelectMany(e => (e.Attendances ?? Enumerable.Empty<AttendanceOutput>())
                .Where(a => !string.IsNullOrWhiteSpace(a.IncidentCode)
                            && a.IncidentCode != "N/A"
                            && a.IncidentCode != "--:--")
                .Select(a => new OnlyIncidentCodeLabel() { IncidentCode = a.IncidentCode, IncidentCodeLabel = a.IncidentCodeLabel }))
            .GroupBy(a => a.IncidentCode)
            .Select(g => g.First())
            .OrderBy(i => i.IncidentCode)
            .ToList();

            Document document = new Document(pdf, pageSize: PageSize.A4.Rotate());
            pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new AttendancePdfHeader(document, companyName, tenantName, typeNom, period, listIncidents, rfcInfo));
            document.SetTopMargin(101);

            foreach (var employee in employeeAttendances)
            {
                var displayName = NameFormatter.Format(employee.Name, employee.LastName, employee.MLastName, nameOrder);
                var table = new Table(listDates.Count + 1).UseAllAvailableWidth();
                table.AddHeaderCell(AddCellToHeadToAttendance(
                    $"Cod. {employee.Codigo} | {displayName} | {employee.Activity}", fontSize, listDates.Count, TextAlignment.LEFT, 5, true, true, true));
                table.AddHeaderCell(AddCellToHeadToAttendance("Firma", fontSize, 1, TextAlignment.CENTER, 1, false, true, true, false));

                int indexDate = 1;
                foreach (var date in listDates)
                {
                    string dateLabel = date.ToString("dd/MM/yy");
                    if (showDayInitial)
                    {
                        string day = date.ToString("ddd", new CultureInfo("es-ES")).TrimEnd('.').ToUpper();
                        string initial = day.Length > 0 ? day.Substring(0, 1) : string.Empty;
                        dateLabel = $"{initial} {dateLabel}";
                    }
                    table.AddHeaderCell(AddCellToHeadDateToAttendance(dateLabel, fontSize, 1, TextAlignment.CENTER, 1, indexDate == 1, false, indexDate == listDates.Count));
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

                    // No spaces around the divider so "entry/exit" is a single token without
                    // break points: iText keeps check-in and check-out together on one line
                    // instead of splitting them when the column is narrow.
                    table.AddCell(AddCellToAttendace($"{checkEntry}{divider}{checkOut}", fontSize, true));
                }
                //header firma
                table.AddHeaderCell(AddCellToHeadToAttendance("", fontSize, 1, TextAlignment.CENTER, 1, false, false, true, false));
                table.AddCell(AddCellToAttendace("", fontSize, true));

                document.Add(table);
            }

            document.Add(new Paragraph("\n").SetMarginTop(5));
            int signatureCount = signatures.Count > 0 ? signatures.Count : 2;
            var tableSignature = new Table(signatureCount).UseAllAvailableWidth();

            for (int i = 0; i < signatureCount; i++)
            {
                tableSignature.AddHeaderCell(AddCellToSignature("______________________________"));
            }

            if (signatures.Count > 0)
            {
                foreach (var sig in signatures)
                {
                    var label = string.IsNullOrWhiteSpace(sig.Position) ? sig.Name : $"{sig.Name}\n{sig.Position}";
                    tableSignature.AddCell(AddCellToSignature(label));
                }
            }
            else
            {
                tableSignature.AddCell(AddCellToSignature("JEFE DE DEPARTAMENTO"));
                tableSignature.AddCell(AddCellToSignature("DIRECCIÓN RECURSOS HUMANOS"));
            }
            document.Add(tableSignature);

            document.Close();

            return memoryStream.ToArray();
        }

        private Cell AddCellToHeadToAttendance(string value, int fontSize, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(fontSize).SetTextAlignment(textAlignment).SetFixedLeading(fontSize)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToHeadDateToAttendance(string value, int fontSize, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(fontSize).SetTextAlignment(textAlignment).SetMaxWidth(80)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToAttendace(string value, int fontSize, bool withBorderBottom = false)
        {
            Cell styledCell = new Cell().SetPadding(1);
            styledCell.Add(new Paragraph(value).SetFontSize(fontSize).SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER).SetMaxWidth(80)).SetPadding(1)
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
