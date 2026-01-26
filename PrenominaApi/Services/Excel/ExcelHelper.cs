using ClosedXML.Excel;

namespace PrenominaApi.Services.Excel
{
    public class ExcelHelper
    {
        public static GeneratedExcel Buid(XLWorkbook workbook, string fileName)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new GeneratedExcel
            {
                FileName = fileName,
                Content = stream.ToArray()
            };
        }
    }
}
