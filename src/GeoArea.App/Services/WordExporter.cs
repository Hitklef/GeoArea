using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;

namespace GeoArea.App.Services;

/// <summary>
/// Зберігає відомість у файл Word (.docx) без сторонніх бібліотек:
/// документ збирається вручну за стандартом Office Open XML і пакується через System.IO.Packaging.
/// </summary>
public static class WordExporter
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string MainPartContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
    private const string MainPartRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

    public static void Save(WorksheetTable table, string path)
    {
        var document = BuildDocument(table);

        using var package = Package.Open(path, FileMode.Create, FileAccess.ReadWrite);
        var partUri = PackUriHelper.CreatePartUri(new Uri("/word/document.xml", UriKind.Relative));
        var part = package.CreatePart(partUri, MainPartContentType, CompressionOption.Normal);
        using (var stream = part.GetStream(FileMode.Create, FileAccess.Write))
        {
            document.Save(stream);
        }

        package.CreateRelationship(partUri, TargetMode.Internal, MainPartRelationship, "rId1");
    }

    private static XDocument BuildDocument(WorksheetTable table)
    {
        var body = new XElement(W + "body");

        body.Add(Paragraph(WorksheetTable.Title, bold: true, size: 28, center: true, spaceAfter: 200));
        body.Add(BuildTable(table));

        if (table.Warnings.Count > 0)
        {
            body.Add(Paragraph(string.Empty));
            foreach (var w in table.Warnings) body.Add(Paragraph("Примітка: " + w, size: 20));
        }

        // Альбомна сторінка A4, поля 1,5 см.
        body.Add(new XElement(W + "sectPr",
            new XElement(W + "pgSz", new XAttribute(W + "w", 16838), new XAttribute(W + "h", 11906), new XAttribute(W + "orient", "landscape")),
            new XElement(W + "pgMar",
                new XAttribute(W + "top", 850), new XAttribute(W + "right", 850),
                new XAttribute(W + "bottom", 850), new XAttribute(W + "left", 850),
                new XAttribute(W + "header", 708), new XAttribute(W + "footer", 708), new XAttribute(W + "gutter", 0))));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W), body));
    }

    private static XElement BuildTable(WorksheetTable table)
    {
        // Ширини стовпців у twips (1/20 pt). Разом ≈ 15 100 twips — ширина альбомної A4 з полями.
        int[] widths = [900, 500, 1700, 500, 1700, 1900, 1900, 2500, 2500];

        var grid = new XElement(W + "tblGrid");
        foreach (var w in widths) grid.Add(new XElement(W + "gridCol", new XAttribute(W + "w", w)));

        var tbl = new XElement(W + "tbl",
            new XElement(W + "tblPr",
                new XElement(W + "tblW", new XAttribute(W + "w", widths.Sum()), new XAttribute(W + "type", "dxa")),
                new XElement(W + "jc", new XAttribute(W + "val", "center")),
                new XElement(W + "tblBorders",
                    Border("top"), Border("left"), Border("bottom"), Border("right"), Border("insideH"), Border("insideV")),
                new XElement(W + "tblCellMar",
                    new XElement(W + "left", new XAttribute(W + "w", 80), new XAttribute(W + "type", "dxa")),
                    new XElement(W + "right", new XAttribute(W + "w", 80), new XAttribute(W + "type", "dxa")))),
            grid);

        // Шапка, рядок 1: № п/п (об'єднано вниз) | Координати (4) | Різниця координат (2) | Добуток (2)
        tbl.Add(Row(header: true,
            Cell("№ п/п", widths[0], bold: true, center: true, vMerge: "restart", shade: true),
            Cell("Координати", widths[1] + widths[2] + widths[3] + widths[4], bold: true, center: true, span: 4, shade: true),
            Cell("Різниця координат", widths[5] + widths[6], bold: true, center: true, span: 2, shade: true),
            Cell("Добуток", widths[7] + widths[8], bold: true, center: true, span: 2, shade: true)));

        // Шапка, рядок 2
        tbl.Add(Row(header: true,
            Cell(string.Empty, widths[0], vMerge: "continue", shade: true),
            Cell("±", widths[1], bold: true, center: true, shade: true),
            Cell("X", widths[2], bold: true, center: true, shade: true),
            Cell("±", widths[3], bold: true, center: true, shade: true),
            Cell("Y", widths[4], bold: true, center: true, shade: true),
            Cell("Xn−1 − Xn+1", widths[5], bold: true, center: true, shade: true),
            Cell("Yn+1 − Yn−1", widths[6], bold: true, center: true, shade: true),
            Cell("(Xn−1 − Xn+1)·Yn", widths[7], bold: true, center: true, shade: true),
            Cell("(Yn+1 − Yn−1)·Xn", widths[8], bold: true, center: true, shade: true)));

        // Шапка, рядок 3: номери стовпців
        var numberCells = new List<XElement>();
        for (int i = 0; i < 9; i++) numberCells.Add(Cell(WorksheetTable.ColumnNumbers[i], widths[i], bold: true, center: true, shade: true));
        tbl.Add(Row(header: true, numberCells.ToArray()));

        // Дані
        foreach (var row in table.Rows)
        {
            var cells = new List<XElement>();
            for (int i = 0; i < 9; i++)
            {
                bool center = i is 0 or 1 or 3;
                cells.Add(Cell(row[i], widths[i], center: center, right: !center));
            }
            tbl.Add(Row(header: false, cells.ToArray()));
        }

        // Сума
        tbl.Add(Row(header: false,
            Cell("Сума", widths[..5].Sum(), bold: true, center: true, span: 5),
            Cell(table.SumRow[5], widths[5], bold: true, right: true),
            Cell(table.SumRow[6], widths[6], bold: true, right: true),
            Cell(table.SumRow[7], widths[7], bold: true, right: true),
            Cell(table.SumRow[8], widths[8], bold: true, right: true)));

        tbl.Add(Row(header: false,
            Cell("Подвоєна площа 2P, м²", widths[..5].Sum(), bold: true, center: true, span: 5),
            Cell(table.DoubleAreaText, widths[5..].Sum(), bold: true, center: true, span: 4)));

        tbl.Add(Row(header: false,
            Cell("Площа P", widths[..5].Sum(), bold: true, center: true, span: 5),
            Cell(table.AreaSquareMetersText + " м² = " + table.AreaHectaresText + " га", widths[5..].Sum(), bold: true, center: true, span: 4)));

        return tbl;
    }

    private static XElement Border(string side) =>
        new(W + side, new XAttribute(W + "val", "single"), new XAttribute(W + "sz", 4), new XAttribute(W + "space", 0), new XAttribute(W + "color", "000000"));

    private static XElement Row(bool header, params XElement[] cells)
    {
        var trPr = new XElement(W + "trPr", new XElement(W + "cantSplit"));
        if (header) trPr.Add(new XElement(W + "tblHeader"));
        return new XElement(W + "tr", trPr, cells);
    }

    private static XElement Cell(string text, int width, bool bold = false, bool center = false, bool right = false, int span = 1, string? vMerge = null, bool shade = false)
    {
        var tcPr = new XElement(W + "tcPr",
            new XElement(W + "tcW", new XAttribute(W + "w", width), new XAttribute(W + "type", "dxa")));
        if (span > 1) tcPr.Add(new XElement(W + "gridSpan", new XAttribute(W + "val", span)));
        if (vMerge is not null)
        {
            var merge = new XElement(W + "vMerge");
            if (vMerge == "restart") merge.Add(new XAttribute(W + "val", "restart"));
            tcPr.Add(merge);
        }
        if (shade) tcPr.Add(new XElement(W + "shd", new XAttribute(W + "val", "clear"), new XAttribute(W + "color", "auto"), new XAttribute(W + "fill", "F1F5F9")));
        tcPr.Add(new XElement(W + "vAlign", new XAttribute(W + "val", "center")));

        string jc = center ? "center" : right ? "right" : "left";
        return new XElement(W + "tc", tcPr, Paragraph(text, bold: bold, size: 20, center: center, justify: jc, spaceAfter: 0));
    }

    private static XElement Paragraph(string text, bool bold = false, int size = 22, bool center = false, string? justify = null, int spaceAfter = 0)
    {
        var pPr = new XElement(W + "pPr",
            new XElement(W + "spacing", new XAttribute(W + "before", 0), new XAttribute(W + "after", spaceAfter)),
            new XElement(W + "jc", new XAttribute(W + "val", justify ?? (center ? "center" : "left"))));

        var rPr = new XElement(W + "rPr",
            new XElement(W + "rFonts", new XAttribute(W + "ascii", "Times New Roman"), new XAttribute(W + "hAnsi", "Times New Roman"), new XAttribute(W + "cs", "Times New Roman")),
            new XElement(W + "sz", new XAttribute(W + "val", size)),
            new XElement(W + "szCs", new XAttribute(W + "val", size)));
        if (bold) rPr.Add(new XElement(W + "b"));

        var t = new XElement(W + "t", text);
        if (text.Length > 0 && (text[0] == ' ' || text[^1] == ' '))
        {
            t.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
        }

        return new XElement(W + "p", pPr, new XElement(W + "r", rPr, t));
    }
}
