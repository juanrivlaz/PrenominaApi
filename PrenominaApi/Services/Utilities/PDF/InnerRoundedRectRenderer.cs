using iText.Layout.Renderer;
using iText.Layout.Element;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Properties;
using iText.Layout;
using iText.Kernel.Pdf;

namespace PrenominaApi.Services.Utilities.PDF
{
    public class InnerRoundedRectRenderer : CellRenderer
    {
        private readonly string _text;
        public InnerRoundedRectRenderer(Cell modelElement, string text): base(modelElement)
        {
            _text = text;
        }

        public override void Draw(DrawContext drawContext)
        {
            base.Draw(drawContext);

            Rectangle cellRect = GetOccupiedAreaBBox();
            float margin = 5f;
            float rectX = cellRect.GetX() + margin;
            float rectY = cellRect.GetY() + margin;
            float rectWidth = 50;//cellRect.GetWidth() - 2 * margin;
            float rectHeight = cellRect.GetHeight() - 2 * margin;
            float radius = 5f;

            PdfCanvas canvas = drawContext.GetCanvas();
            canvas.SaveState();
            canvas.SetFillColor(new DeviceRgb(204, 233, 255));
            canvas.SetColor(new DeviceRgb(25, 27, 31), false);
            // + 25 la mita del with origin de 100
            canvas.RoundRectangle(rectX + 25, rectY, rectWidth, rectHeight, radius);
            canvas.Fill();
            canvas.RestoreState();

            Paragraph p = new Paragraph(_text)
            .SetFontSize(8)
            .SetFontColor(new DeviceRgb(25, 27, 31))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            PdfPage page = drawContext.GetDocument().GetPage(drawContext.GetDocument().GetNumberOfPages());
            Canvas innerCanvas = new Canvas(page, new Rectangle(rectX + 25, rectY, rectWidth, rectHeight));

            innerCanvas.Add(p);
            innerCanvas.Close();
        }
    }
}
