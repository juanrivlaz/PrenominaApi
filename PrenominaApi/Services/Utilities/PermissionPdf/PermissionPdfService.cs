using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Layout.Borders;

namespace PrenominaApi.Services.Utilities.PermissionPdf
{
    public class PermissionPdfService
    {
        public byte[] Generate(string company, string employeeName, string employeeCode, string activity, string department, string date, string permissionLabel, string note, string startDate, string endDate, string totalDays)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using PdfWriter writer = new PdfWriter(memoryStream);
            using PdfDocument pdfDocument = new PdfDocument(writer);

            //var pageSize = PageSize.A4.Rotate();
            Document document = new Document(pdfDocument);//, pageSize);
            document.SetTopMargin(40);

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Título
            document.Add(new Paragraph(company)
                .SetFont(font)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPaddingBottom(0)
                .SetMarginBottom(0)
                .SetFixedLeading(16)
            );
            document.Add(new Paragraph("PERMISO PARA AUNSENTARSE DEL TRABAJO")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetMarginBottom(20)
                .SetFixedLeading(12)
            );

            document.Add(new Div().SetHeight(20));

            var table = new Table(new float[] { 1, 1 })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

            table.AddCell(new Cell()
                .Add(new Paragraph("Nombre: ").Add(new Text(employeeName).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT));

            table.AddCell(new Cell()
                .Add(new Paragraph("Fecha: ")
                .Add(new Text("25/02/2026").SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell()
                .Add(new Paragraph("Código: ")
                .Add(new Text(employeeCode).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT));

            table.AddCell(new Cell() 
                .Add(new Paragraph("")
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell()
                .Add(new Paragraph("Puesto: ")
                .Add(new Text(activity).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT));

            table.AddCell(new Cell()
                .Add(new Paragraph("")
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell()
                .Add(new Paragraph("Departamento: ")
                .Add(new Text(department).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT));

            table.AddCell(new Cell()
                .Add(new Paragraph("")
                .SetFont(font)
                .SetFontSize(12)
                .SetFixedLeading(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);

            document.Add(new Div().SetHeight(30));

            document.Add(new Paragraph("Por medio del presente documento solicito el siguiente permiso:")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetMarginBottom(12)
                .SetFixedLeading(12)
            );

            document.Add(new Paragraph(permissionLabel)
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetMarginBottom(20)
                .SetFixedLeading(12)
            );

            document.Add(new Paragraph("MOTIVOS / OBSERVACIONES / RAZONES:")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.LEFT)
            );

            document.Add(new Paragraph(note)
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetMarginBottom(20)
                .SetFixedLeading(12)
                .SetMultipliedLeading(1.3f)
            //.SetUnderline(1f, -2f)
            );

            document.Add(new Div().SetHeight(20));

            document.Add(new Paragraph("Fecha Inicio: ")
                .Add(new Text(startDate).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetFixedLeading(12)
            );
            document.Add(new Paragraph("Fecha Regreso: ")
                .Add(new Text(endDate).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetFixedLeading(12)
            );
            document.Add(new Paragraph("Total de Días: ")
                .Add(new Text(totalDays).SetFont(fontBold))
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPaddingTop(0)
                .SetPaddingTop(0)
                .SetMarginBottom(20)
                .SetFixedLeading(12)
            );

            document.Add(new Div().SetHeight(190));

            var tableSignratures = new Table(new float[] { 1, 1, 1 })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

            tableSignratures.AddCell(new Cell()
                .Add(new Paragraph("Firma Jefe Depto:")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(12))
                .SetUnderline(1.3f, 15f)
                .SetBorder(Border.NO_BORDER));

            tableSignratures.AddCell(new Cell()
                .Add(new Paragraph("Firma Empleado:")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(12))
                .SetUnderline(1.3f, 15f)
                .SetBorder(Border.NO_BORDER));

            tableSignratures.AddCell(new Cell()
                .Add(new Paragraph("VO BO Depto RH:")
                .SetFont(font)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(12))
                .SetUnderline(1.3f, 15f)
                .SetBorder(Border.NO_BORDER));

            document.Add(tableSignratures);

            document.Close();
            return memoryStream.ToArray();
        }
    }
}
