using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Properties;

namespace PrenominaApi.Services.Utilities.ReportPdf
{
    /// <summary>
    /// Encabezado de los reportes tabulares en PDF. Replica el formato del reporte de asistencia
    /// (empresa, RFC, tenant, tipo de nómina y periodo) sin la tabla de incidencias.
    /// </summary>
    public class ReportPdfHeader : AbstractPdfDocumentEventHandler
    {
        private readonly Document doc;
        private readonly PdfFont font;
        private readonly PdfFont fontBold;
        private readonly DateTime now;
        private readonly string companyName;
        private readonly string rfcInfo;
        private readonly string tenantName;
        private readonly string typeNom;
        private readonly string period;
        private readonly string reportLabel;

        public ReportPdfHeader(
            Document doc,
            string companyName,
            string tenantName,
            string typeNom,
            string period,
            string rfcInfo,
            string reportLabel
        )
        {
            this.doc = doc;
            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            now = DateTime.Now;
            this.companyName = companyName;
            this.tenantName = tenantName;
            this.period = period;
            this.rfcInfo = rfcInfo;
            this.typeNom = typeNom;
            this.reportLabel = reportLabel;
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            Rectangle pageSize = docEvent.GetPage().GetPageSize();

            float coorRight = pageSize.GetRight() - doc.GetRightMargin();
            float coorLeft = pageSize.GetLeft() + doc.GetLeftMargin();
            float coorTop = pageSize.GetTop() - 25;

            float coordX = coorLeft + coorRight;
            float coordXCenter = coordX / 2;
            float footerY = doc.GetBottomMargin();

            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);

            PdfDocument pdfDoc = docEvent.GetDocument();
            PdfPage page = docEvent.GetPage();
            int pageNumber = pdfDoc.GetPageNumber(page);

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned(reportLabel, coorRight, coorTop, TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned(now.ToString("d 'de' MMMM, yyyy", new System.Globalization.CultureInfo("es-ES")), coorRight, coorTop - 10, TextAlignment.RIGHT)
                .Close();

            canvas.SetFont(fontBold!)
                .SetFontSize(10)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(companyName, coorLeft, coorTop - 2, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(rfcInfo, coorLeft, coorTop - 13, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(9)
                .SetFontColor(new DeviceRgb(24, 29, 39))
                .ShowTextAligned(tenantName, coorLeft, coorTop - 28, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("T. Nómina:", coorLeft, coorTop - 46, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(typeNom, coorLeft + 45, coorTop - 46, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(16, 24, 40))
                .ShowTextAligned("Periodo:", coorLeft, coorTop - 59, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .SetFontColor(new DeviceRgb(102, 112, 133))
                .ShowTextAligned(period, coorLeft + 45, coorTop - 59, TextAlignment.LEFT)
                .Close();

            canvas.SetFont(font!)
                .SetFontSize(8)
                .ShowTextAligned($"Página {pageNumber}", coordXCenter, footerY - 20, TextAlignment.CENTER)
                .Close();
        }
    }
}
