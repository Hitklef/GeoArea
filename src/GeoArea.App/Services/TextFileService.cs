using System.IO;
using System.Text;

namespace GeoArea.App.Services;

/// <summary>Читання текстових файлів з координатами (UTF-8 або старе кодування Windows-1251).</summary>
public static class TextFileService
{
    public static string ReadAllText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        // Спочатку пробуємо UTF-8 (суворо, щоб виявити помилкові байти).
        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return strict.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // Не UTF-8 — найімовірніше Windows-1251 (файли зі старих програм або Excel «CSV»).
            return Encoding.GetEncoding(1251).GetString(bytes);
        }
    }
}
