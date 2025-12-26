using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;

namespace PrenominaApi.Services.Utilities.ContractPdf
{
    public class ContractPdfHeader : AbstractPdfDocumentEventHandler
    {
        private PdfFont font;
        private PdfFont fontBold;
        private DateTime now;

        protected Document doc;
        protected string companyName;
        protected string rfc;

        public ContractPdfHeader(Document doc, string companyName, string rfc)
        {
            this.doc = doc;
            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            now = DateTime.Now;
            this.companyName = companyName;
            this.rfc = rfc;
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
            Rectangle pageSize = docEvent.GetPage().GetPageSize();

            float coordRight = pageSize.GetRight() - doc.GetRightMargin();
            float coordLeft = pageSize.GetLeft() + doc.GetLeftMargin();
            float coordTop = pageSize.GetTop() - 44;
            float coordX = coordLeft + coordRight;
            float coordXCenter = coordX / 2;
            float headerY = pageSize.GetTop() - doc.GetTopMargin() + 10;
            float footerY = doc.GetBottomMargin();
            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);
            PdfDocument pdfDoc = docEvent.GetDocument();
            PdfPage page = docEvent.GetPage();
            int pageNumber = pdfDoc.GetPageNumber(page);

            canvas.SetFont(font).SetFontSize(8).ShowTextAligned("Reporte de Contratos", coordRight, coordTop, iText.Layout.Properties.TextAlignment.RIGHT).Close();
            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned(now.ToString("d 'de' MMMM, yyyy", new System.Globalization.CultureInfo("es-ES")), coordRight, coordTop - 10, iText.Layout.Properties.TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(fontBold!)
                .SetFontSize(10)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(companyName, coordLeft, coordTop - 2, iText.Layout.Properties.TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(rfc, coordLeft, coordTop - 13, iText.Layout.Properties.TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned($"Página {pageNumber}", coordXCenter, footerY - 20, iText.Layout.Properties.TextAlignment.CENTER)
                .Close();
        }
    }
}
