using GeoArea.Core;

namespace GeoArea.App.ViewModels;

/// <summary>Один рядок таблиці введення координат (крок 1).</summary>
public sealed class PointRowViewModel : ObservableObject
{
    private int _number;
    private string _xText = string.Empty;
    private string _yText = string.Empty;

    public PointRowViewModel()
    {
    }

    public PointRowViewModel(Vertex vertex, int decimals)
    {
        _xText = vertex.X.ToString("F" + decimals);
        _yText = vertex.Y.ToString("F" + decimals);
    }

    /// <summary>Порядковий номер (перераховується після кожної зміни списку).</summary>
    public int Number
    {
        get => _number;
        set => SetProperty(ref _number, value);
    }

    public string XText
    {
        get => _xText;
        set
        {
            if (SetProperty(ref _xText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(IsXValid));
                OnPropertyChanged(nameof(XState));
            }
        }
    }

    public string YText
    {
        get => _yText;
        set
        {
            if (SetProperty(ref _yText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(IsYValid));
                OnPropertyChanged(nameof(YState));
            }
        }
    }

    public bool IsXValid => CoordinateParser.TryParseNumber(_xText, out _);
    public bool IsYValid => CoordinateParser.TryParseNumber(_yText, out _);

    /// <summary>Порожнє поле — ще не помилка, лише не заповнене; помилка — коли введено не число.</summary>
    public string XState => string.IsNullOrWhiteSpace(_xText) || IsXValid ? "Ok" : "Invalid";
    public string YState => string.IsNullOrWhiteSpace(_yText) || IsYValid ? "Ok" : "Invalid";

    public bool IsEmpty => string.IsNullOrWhiteSpace(_xText) && string.IsNullOrWhiteSpace(_yText);

    public bool TryGetVertex(out Vertex vertex)
    {
        if (CoordinateParser.TryParseNumber(_xText, out var x) && CoordinateParser.TryParseNumber(_yText, out var y))
        {
            vertex = new Vertex(x, y);
            return true;
        }

        vertex = default;
        return false;
    }
}
