using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services.Utilities;

namespace PrenominaApi.Services.Utilities.PDF
{
    public class HeaderAndFooterHandlerAttendace : AbstractPdfDocumentEventHandler
    {
        private const float LogoSize = 50f;
        private const float LogoTextGap = 8f;

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

        public HeaderAndFooterHandlerAttendace(
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
            float headerY = pageSize.GetTop() - doc.GetTopMargin() + 10;
            float footerY = doc.GetBottomMargin();
            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);

            PdfDocument pdfDoc = docEvent.GetDocument();
            PdfPage page = docEvent.GetPage();
            int pageNumber = pdfDoc.GetPageNumber(page);

            float marginX = 180;
            float availableWidth = pageSize.GetWidth() - 215;

            Table tagTable = new Table(6)
                .SetWidth(availableWidth)
                .SetFixedPosition(marginX, coorTop - 45, availableWidth);

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
                        .SetPadding(1))
                    .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
                    .SetTextAlignment(TextAlignment.CENTER);

                tagTable.AddCell(cell);
            }

            canvas.Add(tagTable);

            float textLeft = coorLeft;
            var logoImage = LogoHelper.BuildPdfImage(logoDataUrl, maxWidth: LogoSize, maxHeight: LogoSize);
            if (logoImage != null)
            {
                var logoTable = new Table(1)
                    .SetWidth(LogoSize)
                    .SetFixedPosition(coorLeft, coorTop - (LogoSize - 5), LogoSize);
                logoTable.AddCell(new Cell()
                    .Add(logoImage)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(0));
                canvas.Add(logoTable);
                textLeft = coorLeft + LogoSize + LogoTextGap;
            }

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned("Reporte Tarjeta de Asistencia", coorRight, coorTop, TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned(now.ToString("d 'de' MMMM, yyyy", new System.Globalization.CultureInfo("es-ES")), coorRight, coorTop - 10, TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(fontBold!)
                .SetFontSize(10)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(companyName, textLeft, coorTop - 2, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(rfcInfo, textLeft, coorTop - 13, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(9)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(tenantName, textLeft, coorTop - 28, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("T. Nómina:", textLeft, coorTop - 46, TextAlignment.LEFT)
                .Close();
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(typeNom, textLeft + 45, coorTop - 46, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("Periodo:", textLeft, coorTop - 59, TextAlignment.LEFT)
                .Close();
            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(period, textLeft + 45, coorTop - 59, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned($"Página {pageNumber}", coordXCenter, footerY - 20, TextAlignment.CENTER)
                .Close();
        }
    }
}
