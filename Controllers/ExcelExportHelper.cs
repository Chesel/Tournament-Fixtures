using System.IO;
using System.Collections.Generic;
using ClosedXML.Excel;
using System;

public static class ExcelExportHelper
{
    public static byte[] ExportToExcel<T>(List<T> data, Dictionary<string, Func<T, object>> columnMappings, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        int colIndex = 1;

        // Set headers
        foreach (var column in columnMappings.Keys)
        {
            worksheet.Cell(1, colIndex).Value = column;
            colIndex++;
        }

        // Fill data
        for (int row = 0; row < data.Count; row++)
        {
            int column = 1;
            foreach (var mapping in columnMappings.Values)
            {
                var value = mapping(data[row]);
                worksheet.Cell(row + 2, column).Value = value?.ToString() ?? "";
                column++;
            }
        }


        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
