using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GeoArea.App.Services;

/// <summary>Друк відомості через стандартне вікно друку Windows (альбомна орієнтація).</summary>
public static class PrintService
{
    /// <summary>Повертає true, якщо користувач підтвердив друк.</summary>
    public static bool Print(WorksheetTable table)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return false;

        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

        var document = BuildDocument(table);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PagePadding = new Thickness(40);
        document.ColumnWidth = double.PositiveInfinity;

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "GeoArea — відомість обчислення площі");
        return true;
    }

    /// <summary>Будує документ для друку; доступний також для попереднього перегляду.</summary>
    public static FlowDocument BuildDocument(WorksheetTable table)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 11,
            Foreground = Brushes.Black
        };

        doc.Blocks.Add(new Paragraph(new Run(WorksheetTable.Title))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var tbl = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1, 1, 0, 0) };
        double[] widths = [45, 25, 85, 25, 85, 95, 95, 125, 125];
        foreach (var w in widths) tbl.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });

        var group = new TableRowGroup();
        tbl.RowGroups.Add(group);

        // Шапка
        var h1 = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)) };
        h1.Cells.Add(HeaderCell("№ п/п", rowSpan: 2));
        h1.Cells.Add(HeaderCell("Координати", colSpan: 4));
        h1.Cells.Add(HeaderCell("Різниця координат", colSpan: 2));
        h1.Cells.Add(HeaderCell("Добуток", colSpan: 2));
        group.Rows.Add(h1);

        var h2 = new TableRow { Background = h1.Background };
        foreach (var t in new[] { "±", "X", "±", "Y", "Xn−1 − Xn+1", "Yn+1 − Yn−1", "(Xn−1 − Xn+1)·Yn", "(Yn+1 − Yn−1)·Xn" })
            h2.Cells.Add(HeaderCell(t));
        group.Rows.Add(h2);

        var h3 = new TableRow { Background = h1.Background };
        foreach (var n in WorksheetTable.ColumnNumbers) h3.Cells.Add(HeaderCell(n));
        group.Rows.Add(h3);

        // Дані
        foreach (var row in table.Rows)
        {
            var tr = new TableRow();
            for (int i = 0; i < row.Length; i++)
            {
                bool center = i is 0 or 1 or 3;
                tr.Cells.Add(DataCell(row[i], center ? TextAlignment.Center : TextAlignment.Right));
            }
            group.Rows.Add(tr);
        }

        // Підсумки
        var sum = new TableRow();
        sum.Cells.Add(DataCell("Сума", TextAlignment.Center, bold: true, colSpan: 5));
        for (int i = 5; i < table.SumRow.Length; i++) sum.Cells.Add(DataCell(table.SumRow[i], TextAlignment.Right, bold: true));
        group.Rows.Add(sum);

        var dbl = new TableRow();
        dbl.Cells.Add(DataCell("Подвоєна площа 2P, м²", TextAlignment.Center, bold: true, colSpan: 5));
        dbl.Cells.Add(DataCell(table.DoubleAreaText, TextAlignment.Center, bold: true, colSpan: 4));
        group.Rows.Add(dbl);

        var area = new TableRow();
        area.Cells.Add(DataCell("Площа P", TextAlignment.Center, bold: true, colSpan: 5));
        area.Cells.Add(DataCell(table.AreaSquareMetersText + " м² = " + table.AreaHectaresText + " га", TextAlignment.Center, bold: true, colSpan: 4));
        group.Rows.Add(area);

        doc.Blocks.Add(tbl);

        foreach (var w in table.Warnings)
        {
            doc.Blocks.Add(new Paragraph(new Run("Примітка: " + w)) { FontSize = 10, Margin = new Thickness(0, 6, 0, 0) });
        }

        return doc;
    }

    private static TableCell HeaderCell(string text, int colSpan = 1, int rowSpan = 1) =>
        new(new Paragraph(new Run(text)) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(0) })
        {
            ColumnSpan = colSpan,
            RowSpan = rowSpan,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(4, 3, 4, 3)
        };

    private static TableCell DataCell(string text, TextAlignment align, bool bold = false, int colSpan = 1) =>
        new(new Paragraph(new Run(text)) { TextAlignment = align, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0) })
        {
            ColumnSpan = colSpan,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(4, 2, 4, 2)
        };
}
