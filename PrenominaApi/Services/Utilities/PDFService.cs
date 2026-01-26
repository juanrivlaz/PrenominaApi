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
using PrenominaApi.Services.Utilities.PDF;
using System.Globalization;

namespace PrenominaApi.Services.Utilities
{
    public class PDFService
    {
        public byte[] GeneratePDOM(
            IEnumerable<WorkedDayOffs> workedDayOffs,
            string companyName,
            string tenantName,
            string typeNom,
            string period,
            string rfcInfo
        ) {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Crear un escritor PDF usando el MemoryStream
                using (PdfWriter writer = new PdfWriter(memoryStream))
                {
                    // Configurar para que el stream se mantenga abierto
                    writer.SetCloseStream(false);

                    // Crear el documento PDF
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        // Crear un objeto Document para agregar contenido
                        Document document = new Document(pdf);
                        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new HeaderAndFooterHandlerPDOM(document, companyName, tenantName, typeNom, period, rfcInfo));
                        document.SetTopMargin(115);

                        // Agregar una tabla
                        var table = new Table(8).UseAllAvailableWidth();
                        table.AddHeaderCell(AddCellToHead("Código"));
                        table.AddHeaderCell(AddCellToHead("Nombre"));
                        table.AddHeaderCell(AddCellToHead("Actividad"));
                        table.AddHeaderCell(AddCellToHead("Sueldo"));
                        table.AddHeaderCell(AddCellToHead("Fecha"));
                        table.AddHeaderCell(AddCellToHead("N.Concepto"));
                        table.AddHeaderCell(AddCellToHead("Horas"));
                        table.AddHeaderCell(AddCellToHead("Importe"));

                        //valores
                        int index = 0;
                        foreach (var item in workedDayOffs)
                        {
                            bool applyBgColor = index % 2 == 0;
                            table.AddCell(AddCellToTable(item.EmployeeCode.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeName, applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeActivity, applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeSalary.ToString("C"), applyBgColor));
                            table.AddCell(AddCellToTable(item.Date.ToString("dd/MM/yyyy"), applyBgColor));
                            table.AddCell(AddCellToTable(item.NumConcept, applyBgColor));
                            table.AddCell(AddCellToTable(item.Hours.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable(item.Amount.ToString("C"), applyBgColor));

                            index++;
                        }

                        document.Add(table);
                        // Cerrar el documento
                        document.Close();
                    }
                }

                // Retornar los bytes del MemoryStream
                return memoryStream.ToArray();
            }
        }

        public byte[] GenerateWorkedDayOff(
            IEnumerable<WorkedDayOffs> workedDayOffs,
            string companyName,
            string tenantName,
            string date,
            string rfcInfo
        ) {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Crear un escritor PDF usando el MemoryStream
                using (PdfWriter writer = new PdfWriter(memoryStream))
                {
                    // Configurar para que el stream se mantenga abierto
                    writer.SetCloseStream(false);

                    // Crear el documento PDF
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        // Crear un objeto Document para agregar contenido
                        Document document = new Document(pdf);
                        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new HeaderAndFooterHandlerWorkedDayoff(document, companyName, tenantName, date, rfcInfo));
                        document.SetTopMargin(115);

                        // Agregar una tabla de ejemplo
                        var table = new Table(8).UseAllAvailableWidth();
                        table.AddHeaderCell(AddCellToHead("Código"));
                        table.AddHeaderCell(AddCellToHead("Nombre"));
                        table.AddHeaderCell(AddCellToHead("Actividad"));
                        table.AddHeaderCell(AddCellToHead("Sueldo"));
                        table.AddHeaderCell(AddCellToHead("Fecha"));
                        table.AddHeaderCell(AddCellToHead("N.Concepto"));
                        table.AddHeaderCell(AddCellToHead("Horas"));
                        table.AddHeaderCell(AddCellToHead("Importe"));

                        //valores
                        int index = 0;
                        foreach (var item in workedDayOffs)
                        {
                            bool applyBgColor = index % 2 == 0;
                            table.AddCell(AddCellToTable(item.EmployeeCode.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeName, applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeActivity, applyBgColor));
                            table.AddCell(AddCellToTable(item.EmployeeSalary.ToString("C"), applyBgColor));
                            table.AddCell(AddCellToTable(item.Date.ToString("dd/MM/yyyy"), applyBgColor));
                            table.AddCell(AddCellToTable(item.NumConcept, applyBgColor));
                            table.AddCell(AddCellToTable(item.Hours.ToString(), applyBgColor));
                            table.AddCell(AddCellToTable(item.Amount.ToString("C"), applyBgColor));

                            index++;
                        }

                        document.Add(table);
                        // Cerrar el documento
                        document.Close();
                    }
                }

                // Retornar los bytes del MemoryStream
                return memoryStream.ToArray();
            }
        }

        public byte[] GenerateAttendance(
            IEnumerable<EmployeeAttendancesOutput> employeeAttendances,
            string companyName,
            string tenantName,
            string period,
            List<DateOnly> listDates,
            string rfcInfo,
            string typeNom
        ) {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (PdfWriter writer = new PdfWriter(memoryStream))
                {
                    writer.SetCloseStream(false);

                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        var incidentsApply = SysConfig.IncidentApplyToAttendance;
                        List<OnlyIncidentCodeLabel> listIncidents = employeeAttendances
                        .Where(e => e.Attendances != null && e.Attendances.Any()).SelectMany(e => (e.Attendances ?? Enumerable.Empty<AttendanceOutput>())
                        .Where(a => incidentsApply.Contains(a.IncidentCode))
                            .Select(a => new OnlyIncidentCodeLabel() { IncidentCode = a.IncidentCode, IncidentCodeLabel = a.IncidentCodeLabel }))
                        .GroupBy(a => new OnlyIncidentCodeLabel() { IncidentCode = a.IncidentCode, IncidentCodeLabel = a.IncidentCodeLabel }).Select(g => g.First()).ToList();

                        Document document = new Document(pdf, pageSize: PageSize.A4.Rotate());
                        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new HeaderAndFooterHandlerAttendace(document, companyName, tenantName, typeNom, period, listIncidents, rfcInfo));
                        document.SetTopMargin(120);

                        foreach (var employee in employeeAttendances)
                        {
                            //document.Add(new Paragraph($"Cod. {employee.Codigo} | {employee.Name} {employee.LastName} {employee.MLastName} | {employee.Activity}").SetFontSize(8).SetTextAlignment(TextAlignment.LEFT));
                            
                            var table = new Table(listDates.Count + 1).UseAllAvailableWidth();
                            table.AddHeaderCell(AddCellToHeadToAttendance(
                                $"Cod. {employee.Codigo} | {employee.Name} {employee.LastName} {employee.MLastName} | {employee.Activity}", listDates.Count, TextAlignment.LEFT, 10, true, true, true));
                            table.AddHeaderCell(AddCellToHeadToAttendance("Observación", 1, TextAlignment.CENTER, 1, false, true, true, false));

                            int indexDate = 1;
                            foreach (var date in listDates)
                            {
                                string day = date.ToString("ddd", new CultureInfo("es-ES")).ToUpper();
                                table.AddHeaderCell(AddCellToHeadDateToAttendance($"{day}\n{date.ToString("dd/MM/yy")}", 1, TextAlignment.CENTER, 1, indexDate == 1, false, indexDate == listDates.Count));
                                indexDate++;

                                //entry
                                var attendace = employee.Attendances?.FirstOrDefault(a => a.Date == date);
                                table.AddCell(AddCellToAttendace(attendace?.CheckEntry ?? attendace?.IncidentCode ?? "N/A", true));
                            }
                            //header observation
                            table.AddHeaderCell(AddCellToHeadToAttendance("", 1, TextAlignment.CENTER, 1, false, false, true, false));
                            table.AddCell(AddCellToAttendace("", false));

                            foreach (var date in listDates)
                            {
                                //out
                                var attendace = employee.Attendances?.FirstOrDefault(a => a.Date == date);
                                table.AddCell(AddCellToAttendace(attendace?.CheckOut ?? "N/A", true));
                            }
                            //observation
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
                    }
                }

                // Retornar los bytes del MemoryStream
                return memoryStream.ToArray();
            }
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

        private Cell AddCellToHead(string value)
        {
            return new Cell().Add(
                new Paragraph(value).SetFontSize(8)
            ).SetPadding(6)
            .SetBackgroundColor(new DeviceRgb(236, 240, 243))
            .SetBorderLeft(Border.NO_BORDER)
            .SetBorderRight(Border.NO_BORDER)
            .SetBorderTop(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f));
        }

        private Cell AddCellToHeadToAttendance(string value, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(8).SetTextAlignment(textAlignment)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToHeadDateToAttendance(string value, int colspan = 1, TextAlignment textAlignment = TextAlignment.CENTER, int padding = 1, bool borderLeft = false, bool borderTop = false, bool borderRight = false, bool borderBottom = false)
        {
            return new Cell(1, colspan).Add(
                new Paragraph(value).SetFontSize(8).SetTextAlignment(textAlignment).SetMaxWidth(80)
            ).SetPadding(padding)
            .SetBorderBottom(borderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderLeft(borderLeft ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderTop(borderTop ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER)
            .SetBorderRight(borderRight ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
        }

        private Cell AddCellToAttendace(string value, bool withBorderBottom = false)
        {
            Cell styledCell = new Cell().SetPadding(1);
            styledCell.Add(new Paragraph(value).SetFontSize(8).SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER).SetMaxWidth(80)).SetPadding(1)
            .SetBorderLeft(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
            .SetBorderRight(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
            .SetBorderTop(Border.NO_BORDER)
            .SetBorderBottom(withBorderBottom ? new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f) : Border.NO_BORDER);
            //styledCell.SetNextRenderer(new InnerRoundedRectRenderer(styledCell, value));

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
