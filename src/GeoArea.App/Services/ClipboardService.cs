using System.Net;
using System.Text;
using System.Windows;

namespace GeoArea.App.Services;

/// <summary>Копіює відомість у буфер обміну одразу у двох виглядах: як текст із табуляцією (для Excel) і як HTML-таблицю (для Word).</summary>
public static class ClipboardService
{
    public static void CopyWorksheet(WorksheetTable table)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, BuildTsv(table));
        data.SetData(DataFormats.Html, BuildCfHtml(BuildHtml(table)));
        Clipboard.SetDataObject(data, copy: true);
    }

    private static string BuildTsv(WorksheetTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(WorksheetTable.Title);
        sb.AppendLine(string.Join('\t', WorksheetTable.FlatHeaders));
        sb.AppendLine(string.Join('\t', WorksheetTable.ColumnNumbers));
        foreach (var row in table.Rows) sb.AppendLine(string.Join('\t', row));
        sb.AppendLine(string.Join('\t', table.SumRow));
        sb.AppendLine("Подвоєна площа 2P, м²\t\t\t\t\t\t\t" + table.DoubleAreaText);
        sb.AppendLine("Площа P, м²\t\t\t\t\t\t\t" + table.AreaSquareMetersText);
        sb.AppendLine("Площа P, га\t\t\t\t\t\t\t" + table.AreaHectaresText);
        return sb.ToString();
    }

    private static string BuildHtml(WorksheetTable table)
    {
        static string E(string s) => WebUtility.HtmlEncode(s);
        const string td = "style=\"border:1px solid #000;padding:2px 6px;text-align:right;white-space:nowrap\"";
        const string th = "style=\"border:1px solid #000;padding:2px 6px;text-align:center;font-weight:bold\"";

        var sb = new StringBuilder();
        sb.Append("<p style=\"text-align:center;font-weight:bold;font-size:14pt\">").Append(E(WorksheetTable.Title)).Append("</p>");
        sb.Append("<table style=\"border-collapse:collapse;font-family:'Times New Roman';font-size:11pt\">");
        sb.Append("<tr>")
          .Append($"<th rowspan=\"2\" {th}>№ п/п</th>")
          .Append($"<th colspan=\"4\" {th}>Координати</th>")
          .Append($"<th colspan=\"2\" {th}>Різниця координат</th>")
          .Append($"<th colspan=\"2\" {th}>Добуток</th>")
          .Append("</tr><tr>")
          .Append($"<th {th}>±</th><th {th}>X</th><th {th}>±</th><th {th}>Y</th>")
          .Append($"<th {th}>X<sub>n-1</sub> − X<sub>n+1</sub></th>")
          .Append($"<th {th}>Y<sub>n+1</sub> − Y<sub>n-1</sub></th>")
          .Append($"<th {th}>(X<sub>n-1</sub> − X<sub>n+1</sub>)·Y<sub>n</sub></th>")
          .Append($"<th {th}>(Y<sub>n+1</sub> − Y<sub>n-1</sub>)·X<sub>n</sub></th>")
          .Append("</tr><tr>");
        foreach (var n in WorksheetTable.ColumnNumbers) sb.Append($"<th {th}>{n}</th>");
        sb.Append("</tr>");

        foreach (var row in table.Rows)
        {
            sb.Append("<tr>");
            for (int i = 0; i < row.Length; i++)
            {
                string align = i is 0 or 1 or 3 ? "center" : "right";
                sb.Append($"<td style=\"border:1px solid #000;padding:2px 6px;text-align:{align};white-space:nowrap\">").Append(E(row[i])).Append("</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("<tr>")
          .Append($"<td colspan=\"5\" style=\"border:1px solid #000;padding:2px 6px;text-align:center;font-weight:bold\">Сума</td>");
        for (int i = 5; i < table.SumRow.Length; i++) sb.Append($"<td {td}><b>").Append(E(table.SumRow[i])).Append("</b></td>");
        sb.Append("</tr>");
        sb.Append($"<tr><td colspan=\"5\" {th}>Подвоєна площа 2P, м²</td><td colspan=\"4\" {td}><b>").Append(E(table.DoubleAreaText)).Append("</b></td></tr>");
        sb.Append($"<tr><td colspan=\"5\" {th}>Площа P</td><td colspan=\"4\" {td}><b>").Append(E(table.AreaSquareMetersText)).Append(" м² = ").Append(E(table.AreaHectaresText)).Append(" га</b></td></tr>");
        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>Загортає HTML у формат CF_HTML, який розуміють Word і Excel.</summary>
    private static string BuildCfHtml(string fragment)
    {
        const string header =
            "Version:0.9\r\n" +
            "StartHTML:{0:D10}\r\n" +
            "EndHTML:{1:D10}\r\n" +
            "StartFragment:{2:D10}\r\n" +
            "EndFragment:{3:D10}\r\n";

        string pre = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
        string post = "<!--EndFragment--></body></html>";

        string headerPlaceholder = string.Format(header, 0, 0, 0, 0);
        int startHtml = Encoding.UTF8.GetByteCount(headerPlaceholder);
        int startFragment = startHtml + Encoding.UTF8.GetByteCount(pre);
        int endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        int endHtml = endFragment + Encoding.UTF8.GetByteCount(post);

        return string.Format(header, startHtml, endHtml, startFragment, endFragment) + pre + fragment + post;
    }
}
