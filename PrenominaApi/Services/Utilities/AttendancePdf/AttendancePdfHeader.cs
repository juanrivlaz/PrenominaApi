using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Kernel.Geom;
using iText.Layout.Element;
using PrenominaApi.Models.Dto.Output;
using iText.Layout.Properties;
using PrenominaApi.Services.Utilities;

namespace PrenominaApi.Services.Utilities.Attendance
{
    public class AttendancePdfHeader : AbstractPdfDocumentEventHandler
    {
        private const float LogoMaxWidth = 120f;
        private const float LogoMaxHeight = 46f;

        protected Document doc;
        private PdfFont font;
        private PdfFont fontBold;
        private DateTime now;
        private string companyName;
        private string rfcInfo;
        private string tenantName;
        private string typeNom;
        private string period;
        private List<OnlyIncidentCodeLabel> onlyIncidentCodeLabels;
        private string? logoDataUrl;

        public AttendancePdfHeader(
            Document doc,
            string companyName,
            string tenantName,
            string typeNom,
            string period,
            List<OnlyIncidentCodeLabel> onlyIncidentCodeLabels,
            string rfcInfo,
            string? logoDataUrl = null
        )
        {
            this.doc = doc;
            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            now = DateTime.Now;
            this.companyName = companyName;
            this.tenantName = tenantName;
            this.period = period;
            this.onlyIncidentCodeLabels = onlyIncidentCodeLabels;
            this.rfcInfo = rfcInfo;
            this.typeNom = typeNom;
            this.logoDataUrl = logoDataUrl;
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            Rectangle pageSize = docEvent.GetPage().GetPageSize();

            float coorRight = pageSize.GetRight() - doc.GetRightMargin();
            float coorLeft = pageSize.GetLeft() + doc.GetLeftMargin();
            float coorTop = pageSize.GetTop() - 44;

            float coordX = coorLeft + coorRight;
            float coordXCenter = coordX / 2;
            float footerY = doc.GetBottomMargin();
            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);

            PdfDocument pdfDoc = docEvent.GetDocument();
            PdfPage page = docEvent.GetPage();
            int pageNumber = pdfDoc.GetPageNumber(page);

            float pageTop = pageSize.GetTop();
            float xLeft = coorLeft;
            float fullWidth = coorRight - coorLeft;

            // Logo arriba del título (alineado a la izquierda).
            float logoTopY = pageTop - 6;
            var logoImage = LogoHelper.BuildPdfImage(logoDataUrl, maxWidth: LogoMaxWidth, maxHeight: LogoMaxHeight);
            float titleY = logoImage != null ? logoTopY - LogoMaxHeight - 12 : pageTop - 22;
            float rfcY = titleY - 12;
            float infoRowY = rfcY - 18;

            // Tabla de leyenda de incidencias: ocupa todo el ancho desde la izquierda
            // (sin hueco) y se ubica debajo de la fila de información.
            float legendBottomY = infoRowY - 26;
            Table tagTable = new Table(6)
                .SetWidth(fullWidth)
                .SetFixedPosition(xLeft, legendBottomY, fullWidth);

            var distinctList = onlyIncidentCodeLabels
            .GroupBy(x => new { x.IncidentCode, x.IncidentCodeLabel })
            .Select(g => g.First())
            .ToList();

            foreach (var tag in distinctList)
            {
                var cell = new Cell()
                    .Add(new Paragraph($"{tag.IncidentCode} | {tag.IncidentCodeLabel}")
                        .SetFontSize(8)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(1)
                     )
                    .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
                    .SetTextAlignment(TextAlignment.CENTER);

                tagTable.AddCell(cell);
            }

            canvas.Add(tagTable);

            if (logoImage != null)
            {
                var logoTable = new Table(1)
                    .SetWidth(LogoMaxWidth)
                    .SetFixedPosition(xLeft, logoTopY - LogoMaxHeight, LogoMaxWidth);
                logoTable.AddCell(new Cell()
                    .Add(logoImage)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(0));
                canvas.Add(logoTable);
            }

            // Lado derecho: título del reporte y fecha.
            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned("Reporte Tarjeta de Asistencia", coorRight, coorTop, TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned(now.ToString("d 'de' MMMM, yyyy", new System.Globalization.CultureInfo("es-ES")), coorRight, coorTop - 10, TextAlignment.RIGHT)
                .Close();

            // Título de la empresa (debajo del logo).
            canvas.SetFont(fontBold!)
                .SetFontSize(11)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(companyName, xLeft, titleY, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(rfcInfo, xLeft, rfcY, TextAlignment.LEFT)
                .Close();

            // Fila central: centro | T. Nómina | Periodo (en la misma fila).
            canvas.SetFont(font!)
                .SetFontSize(9)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(tenantName, xLeft, infoRowY, TextAlignment.LEFT)
                .Close();

            float nominaLabelX = xLeft + 130;
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("T. Nómina:", nominaLabelX, infoRowY, TextAlignment.LEFT)
                .Close();
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(typeNom, nominaLabelX + 48, infoRowY, TextAlignment.LEFT)
                .Close();

            float periodoLabelX = xLeft + 270;
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("Periodo:", periodoLabelX, infoRowY, TextAlignment.LEFT)
                .Close();
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(period, periodoLabelX + 42, infoRowY, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned($"Página {pageNumber}", coordXCenter, footerY - 20, TextAlignment.CENTER)
                .Close();
        }
    }
}
