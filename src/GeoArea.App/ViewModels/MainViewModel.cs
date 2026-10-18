using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GeoArea.App.Services;
using GeoArea.Core;

namespace GeoArea.App.ViewModels;

/// <summary>Стан головного вікна: точки, налаштування, результат і відомість.</summary>
public sealed class MainViewModel : ObservableObject
{
    private int _decimals = 2;
    private bool _ignoreClosingDuplicate = true;
    private bool _hasResult;
    private string _areaSquareMetersText = "—";
    private string _areaHectaresText = "—";
    private string _doubleAreaText = "—";
    private string _sum6Text = "—";
    private string _sum7Text = "—";
    private string _sum8Text = "—";
    private string _sum9Text = "—";
    private bool _controlPassed;
    private string _controlText = string.Empty;
    private string _messageText = string.Empty;
    private string _warningsText = string.Empty;
    private string _statusText = "Очікую введення координат.";
    private bool _suppressRecalc;
    private bool _isPasteOpen;
    private string _pasteText = string.Empty;

    public MainViewModel()
    {
        Points.CollectionChanged += OnPointsChanged;

        AddPointCommand = new RelayCommand(AddPoint);
        RemovePointCommand = new RelayCommand(p => RemovePoint(p as PointRowViewModel));
        MoveUpCommand = new RelayCommand(p => MovePoint(p as PointRowViewModel, -1), p => CanMove(p as PointRowViewModel, -1));
        MoveDownCommand = new RelayCommand(p => MovePoint(p as PointRowViewModel, +1), p => CanMove(p as PointRowViewModel, +1));

        // Три порожні рядки на старті, щоб було видно, куди вводити.
        for (int i = 0; i < 3; i++) Points.Add(CreateRow());
    }

    // ---------- Дані ----------

    public ObservableCollection<PointRowViewModel> Points { get; } = new();

    public ObservableCollection<WorksheetRowViewModel> WorksheetRows { get; } = new();

    /// <summary>Останній успішний результат (для копіювання, Word, друку).</summary>
    public AreaResult? LastResult { get; private set; }

    public IReadOnlyList<int> DecimalsOptions { get; } = [2, 3, 4];

    public int Decimals
    {
        get => _decimals;
        set
        {
            if (SetProperty(ref _decimals, value)) Recalculate();
        }
    }

    public bool IgnoreClosingDuplicate
    {
        get => _ignoreClosingDuplicate;
        set
        {
            if (SetProperty(ref _ignoreClosingDuplicate, value)) Recalculate();
        }
    }

    // ---------- Результат ----------

    public bool HasResult { get => _hasResult; private set { if (SetProperty(ref _hasResult, value)) OnPropertyChanged(nameof(HasNoResult)); } }
    public bool HasNoResult => !_hasResult;
    public string AreaSquareMetersText { get => _areaSquareMetersText; private set => SetProperty(ref _areaSquareMetersText, value); }
    public string AreaHectaresText { get => _areaHectaresText; private set => SetProperty(ref _areaHectaresText, value); }
    public string DoubleAreaText { get => _doubleAreaText; private set => SetProperty(ref _doubleAreaText, value); }
    public string Sum6Text { get => _sum6Text; private set => SetProperty(ref _sum6Text, value); }
    public string Sum7Text { get => _sum7Text; private set => SetProperty(ref _sum7Text, value); }
    public string Sum8Text { get => _sum8Text; private set => SetProperty(ref _sum8Text, value); }
    public string Sum9Text { get => _sum9Text; private set => SetProperty(ref _sum9Text, value); }
    public bool ControlPassed { get => _controlPassed; private set => SetProperty(ref _controlPassed, value); }
    public string ControlText { get => _controlText; private set => SetProperty(ref _controlText, value); }

    /// <summary>Пояснення, чому результату ще немає (показується замість результату).</summary>
    public string MessageText { get => _messageText; private set => SetProperty(ref _messageText, value); }

    /// <summary>Попередження з розрахунку (замкнений контур, самоперетин тощо); порожньо — немає.</summary>
    public string WarningsText { get => _warningsText; private set { if (SetProperty(ref _warningsText, value)) OnPropertyChanged(nameof(HasWarnings)); } }
    public bool HasWarnings => !string.IsNullOrEmpty(_warningsText);

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public string PointCountText => Points.Count.ToString();

    /// <summary>Чи відкрите поле для вставки координат з буфера.</summary>
    public bool IsPasteOpen
    {
        get => _isPasteOpen;
        set
        {
            if (SetProperty(ref _isPasteOpen, value) && !value) PasteText = string.Empty;
        }
    }

    public string PasteText
    {
        get => _pasteText;
        set => SetProperty(ref _pasteText, value ?? string.Empty);
    }

    // ---------- Команди ----------

    public RelayCommand AddPointCommand { get; }
    public RelayCommand RemovePointCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }

    // ---------- Дії ----------

    public void AddPoint()
    {
        Points.Add(CreateRow());
    }

    /// <summary>Додає розпізнані вершини (з буфера або файлу). Порожні рядки-заготовки при цьому прибираються.</summary>
    public void AddVertices(IEnumerable<Vertex> vertices)
    {
        _suppressRecalc = true;
        try
        {
            for (int i = Points.Count - 1; i >= 0; i--)
            {
                if (Points[i].IsEmpty) Points.RemoveAt(i);
            }

            foreach (var v in vertices)
            {
                Points.Add(CreateRow(v));
            }
        }
        finally
        {
            _suppressRecalc = false;
        }

        Recalculate();
    }

    public void ClearAll()
    {
        _suppressRecalc = true;
        try
        {
            // Clear() не передає старі елементи в подію, тому відписуємось вручну.
            foreach (var row in Points) row.PropertyChanged -= OnRowChanged;
            Points.Clear();
            for (int i = 0; i < 3; i++) Points.Add(CreateRow());
        }
        finally
        {
            _suppressRecalc = false;
        }

        Recalculate();
    }

    private void RemovePoint(PointRowViewModel? row)
    {
        if (row is null) return;
        Points.Remove(row);
    }

    private bool CanMove(PointRowViewModel? row, int direction)
    {
        if (row is null) return false;
        int index = Points.IndexOf(row);
        int target = index + direction;
        return index >= 0 && target >= 0 && target < Points.Count;
    }

    private void MovePoint(PointRowViewModel? row, int direction)
    {
        if (!CanMove(row, direction)) return;
        int index = Points.IndexOf(row!);
        Points.Move(index, index + direction);
    }

    // ---------- Внутрішнє ----------

    private PointRowViewModel CreateRow()
    {
        var row = new PointRowViewModel();
        row.PropertyChanged += OnRowChanged;
        return row;
    }

    private PointRowViewModel CreateRow(Vertex v)
    {
        var row = new PointRowViewModel(v, Decimals);
        row.PropertyChanged += OnRowChanged;
        return row;
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PointRowViewModel.XText) or nameof(PointRowViewModel.YText))
        {
            Recalculate();
        }
    }

    private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PointRowViewModel row in e.OldItems) row.PropertyChanged -= OnRowChanged;
        }

        Renumber();
        OnPropertyChanged(nameof(PointCountText));
        RelayCommand.RaiseCanExecuteChanged();
        if (!_suppressRecalc) Recalculate();
    }

    private void Renumber()
    {
        for (int i = 0; i < Points.Count; i++) Points[i].Number = i + 1;
    }

    /// <summary>Перераховує відомість і результат. Викликається після кожної зміни.</summary>
    public void Recalculate()
    {
        if (_suppressRecalc) return;

        var vertices = new List<Vertex>();
        int invalid = 0;
        foreach (var row in Points)
        {
            if (row.IsEmpty) continue;              // порожні рядки не заважають
            if (row.TryGetVertex(out var v)) vertices.Add(v);
            else invalid++;
        }

        if (invalid > 0)
        {
            SetNoResult("У деяких рядках координати введені не повністю або з помилкою (поле підсвічене червоним). " +
                        "Перевірте, чи в кожному полі стоїть число.");
            StatusText = "Є рядки з помилками — виправте їх, щоб побачити результат.";
            return;
        }

        if (vertices.Count < AreaCalculator.MinimumVertices)
        {
            int missing = AreaCalculator.MinimumVertices - vertices.Count;
            SetNoResult($"Додайте ще {missing} {Plural(missing, "точку", "точки", "точок")} — для розрахунку площі потрібно щонайменше 3.");
            StatusText = $"Введено точок: {vertices.Count}. Потрібно щонайменше 3.";
            return;
        }

        AreaResult result;
        try
        {
            result = AreaCalculator.Calculate(vertices, new AreaOptions(IgnoreClosingDuplicate));
        }
        catch (ArgumentException ex)
        {
            SetNoResult(ex.Message);
            StatusText = "Недостатньо різних точок для розрахунку.";
            return;
        }

        LastResult = result;
        WorksheetRows.Clear();
        foreach (var r in result.Rows) WorksheetRows.Add(new WorksheetRowViewModel(r, Decimals));

        AreaSquareMetersText = NumberFormat.Format(result.AreaSquareMeters, 2);
        AreaHectaresText = NumberFormat.Format(result.AreaHectares, 4);
        DoubleAreaText = NumberFormat.Format(result.DoubleArea, Decimals);
        Sum6Text = NumberFormat.Format(result.SumDeltaX, Decimals);
        Sum7Text = NumberFormat.Format(result.SumDeltaY, Decimals);
        Sum8Text = NumberFormat.Format(result.Sum8, Decimals);
        Sum9Text = NumberFormat.Format(result.Sum9, Decimals);
        ControlPassed = result.ControlPassed;
        ControlText = result.ControlPassed
            ? "Контроль пройдено. Обидва способи підрахунку дали однаковий результат — обчислення правильне."
            : "Контроль не пройдено: суми стовпців 8 і 9 відрізняються. Перевірте координати.";
        WarningsText = string.Join(Environment.NewLine, result.Warnings);
        MessageText = string.Empty;
        HasResult = true;
        StatusText = $"Готово. Точок у розрахунку: {result.Rows.Count}. Площа {AreaSquareMetersText} м² = {AreaHectaresText} га.";
    }

    private void SetNoResult(string message)
    {
        LastResult = null;
        WorksheetRows.Clear();
        HasResult = false;
        MessageText = message;
        WarningsText = string.Empty;
        AreaSquareMetersText = AreaHectaresText = DoubleAreaText = "—";
        Sum6Text = Sum7Text = Sum8Text = Sum9Text = "—";
        ControlPassed = false;
        ControlText = string.Empty;
    }

    private static string Plural(int n, string one, string few, string many)
    {
        int m10 = n % 10, m100 = n % 100;
        if (m10 == 1 && m100 != 11) return one;
        if (m10 is >= 2 and <= 4 && m100 is < 12 or > 14) return few;
        return many;
    }
}
