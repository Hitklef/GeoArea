using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeoArea.App.Services;
using GeoArea.App.ViewModels;
using GeoArea.Core;
using Microsoft.Win32;

namespace GeoArea.App;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    /// <summary>Базовий розмір вікна, під який зверстано інтерфейс (масштаб 1:1).</summary>
    private const double DesignWidth = 1280;
    private const double DesignHeight = 900;

    /// <summary>Нижня межа масштабу, щоб текст лишався читабельним.</summary>
    private const double MinScale = 0.55;

    public MainWindow()
    {
        InitializeComponent();
        WorksheetGrid.LayoutUpdated += (_, _) => SyncSumColumns();
    }

    /// <summary>
    /// Масштабування: якщо вікно менше за базовий розмір, увесь вміст пропорційно зменшується,
    /// тому зовнішній скрол ніколи не потрібен. Більшим за 1 масштаб не робимо — на великих
    /// екранах картки просто отримують більше місця.
    /// </summary>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double scale = Math.Min(1.0, Math.Min(ActualWidth / DesignWidth, ActualHeight / DesignHeight));
        scale = Math.Max(MinScale, scale);

        if (Math.Abs(RootScale.ScaleX - scale) > 0.001)
        {
            RootScale.ScaleX = scale;
            RootScale.ScaleY = scale;
        }
    }

    // ---------- Крок 1 ----------

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        Vm.IsPasteOpen = true;

        // Якщо в буфері вже є текст із числами — одразу підставляємо його, щоб користувачу лишилося натиснути одну кнопку.
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (CoordinateParser.Parse(text).HasVertices) Vm.PasteText = text;
            }
        }
        catch
        {
            // Буфер обміну може бути зайнятий іншою програмою — тоді користувач вставить вручну.
        }

        PasteBox.Focus();
        PasteBox.CaretIndex = PasteBox.Text.Length;
    }

    private void CancelPaste_Click(object sender, RoutedEventArgs e) => Vm.IsPasteOpen = false;

    private void ApplyPaste_Click(object sender, RoutedEventArgs e)
    {
        var parsed = CoordinateParser.Parse(Vm.PasteText);
        if (!parsed.HasVertices)
        {
            MessageBox.Show(this,
                "У вставленому тексті не знайдено жодної пари чисел.\n\n" +
                "Перевірте, що кожна точка записана з нового рядка, а між X та Y є пробіл, кома, крапка з комою або табуляція.",
                "Нічого додати", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Vm.AddVertices(parsed.Vertices);
        Vm.IsPasteOpen = false;
        ReportSkipped(parsed);
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Виберіть файл з координатами",
            Filter = "Текстові файли (*.txt;*.csv)|*.txt;*.csv|Усі файли (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;

        string text;
        try
        {
            text = TextFileService.ReadAllText(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Не вдалося прочитати файл:\n" + ex.Message, "Помилка читання", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var parsed = CoordinateParser.Parse(text);
        if (!parsed.HasVertices)
        {
            MessageBox.Show(this,
                "У файлі не знайдено координат.\n\nОчікується, що в кожному рядку записані X та Y однієї точки, наприклад:\n5432,10  1234,56",
                "Координат не знайдено", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (HasEnteredPoints())
        {
            var answer = MessageBox.Show(this,
                "У таблиці вже є точки. Замінити їх точками з файлу?\n\n«Так» — замінити, «Ні» — додати до наявних.",
                "Як вчинити з наявними точками?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            if (answer == MessageBoxResult.Yes) Vm.ClearAll();
        }

        Vm.AddVertices(parsed.Vertices);
        ReportSkipped(parsed);
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        if (!HasEnteredPoints())
        {
            Vm.ClearAll();
            return;
        }

        var answer = MessageBox.Show(this,
            "Видалити всі введені точки і почати заново?\n\nЦю дію не можна скасувати.",
            "Очистити таблицю?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes) Vm.ClearAll();
    }

    /// <summary>Enter у полі координати переводить фокус на наступне поле; в останньому — додає новий рядок.</summary>
    private void CoordinateBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox box) return;
        e.Handled = true;

        int rowIndex = box.DataContext is PointRowViewModel row ? Vm.Points.IndexOf(row) : -1;
        if (rowIndex < 0) return;
        bool isYField = box.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == nameof(PointRowViewModel.YText);

        if (!isYField)
        {
            FocusCoordinate(rowIndex, isY: true);
            return;
        }

        if (rowIndex == Vm.Points.Count - 1)
        {
            Vm.AddPoint();
        }

        // Новий рядок з'являється в розмітці трохи пізніше, тому фокус ставимо відкладено.
        int next = rowIndex + 1;
        Dispatcher.BeginInvoke(() => FocusCoordinate(next, isY: false), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Ставить курсор у поле X або Y вказаного рядка таблиці точок.</summary>
    private void FocusCoordinate(int rowIndex, bool isY)
    {
        if (rowIndex < 0 || rowIndex >= Vm.Points.Count) return;
        if (PointsList.ItemContainerGenerator.ContainerFromIndex(rowIndex) is not DependencyObject container) return;

        string path = isY ? nameof(PointRowViewModel.YText) : nameof(PointRowViewModel.XText);
        var target = FindDescendants<TextBox>(container)
            .FirstOrDefault(t => t.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == path);
        if (target is null) return;

        target.Focus();
        target.SelectAll();
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) yield return typed;
            foreach (var deeper in FindDescendants<T>(child)) yield return deeper;
        }
    }

    private bool HasEnteredPoints() => Vm.Points.Any(p => !p.IsEmpty);

    private void ReportSkipped(ParseResult parsed)
    {
        if (parsed.SkippedLines.Count == 0) return;
        var sample = string.Join("\n", parsed.SkippedLines.Take(5));
        MessageBox.Show(this,
            $"Додано точок: {parsed.Vertices.Count}.\n\nПропущено рядків без координат: {parsed.SkippedLines.Count} (заголовки або текст), наприклад:\n{sample}",
            "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------- Крок 3 ----------

    private WorksheetTable? CurrentTable()
    {
        if (Vm.LastResult is null)
        {
            MessageBox.Show(this, "Спочатку введіть щонайменше три точки без помилок — тоді відомість буде готова.", "Немає результату", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        return WorksheetTable.Build(Vm.LastResult, Vm.Decimals);
    }

    private void CopyTable_Click(object sender, RoutedEventArgs e)
    {
        var table = CurrentTable();
        if (table is null) return;

        try
        {
            ClipboardService.CopyWorksheet(table);
            MessageBox.Show(this,
                "Відомість скопійовано.\n\nТепер відкрийте Word або Excel і натисніть Ctrl+V (або правою кнопкою миші → «Вставити»).",
                "Скопійовано", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не вдалося скопіювати в буфер обміну:\n" + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveWord_Click(object sender, RoutedEventArgs e)
    {
        var table = CurrentTable();
        if (table is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Зберегти відомість у Word",
            Filter = "Документ Word (*.docx)|*.docx",
            FileName = $"Відомість площі {DateTime.Now:yyyy-MM-dd}.docx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            WordExporter.Save(table, dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                "Не вдалося зберегти файл:\n" + ex.Message + "\n\nЯкщо файл з такою назвою відкритий у Word — закрийте його і спробуйте ще раз.",
                "Помилка збереження", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var open = MessageBox.Show(this,
            $"Файл збережено:\n{dialog.FileName}\n\nВідкрити його зараз?",
            "Збережено", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes);
        if (open == MessageBoxResult.Yes)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show(this, "Не вдалося відкрити файл автоматично. Відкрийте його вручну з папки, куди зберегли.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var table = CurrentTable();
        if (table is null) return;

        try
        {
            PrintService.Print(table);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не вдалося надрукувати:\n" + ex.Message, "Помилка друку", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var help = new HelpWindow { Owner = this };
        help.ShowDialog();
    }

    /// <summary>Вирівнює стовпці рядків підсумків під фактичні ширини стовпців таблиці.</summary>
    private void SyncSumColumns()
    {
        if (WorksheetGrid.Columns.Count != SumsGrid.ColumnDefinitions.Count) return;
        for (int i = 0; i < WorksheetGrid.Columns.Count; i++)
        {
            double w = WorksheetGrid.Columns[i].ActualWidth;
            if (w > 0 && Math.Abs(SumsGrid.ColumnDefinitions[i].Width.Value - w) > 0.5)
            {
                SumsGrid.ColumnDefinitions[i].Width = new GridLength(w);
            }
        }
    }
}
