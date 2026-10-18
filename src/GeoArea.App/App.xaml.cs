using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace GeoArea.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Українські формати чисел ("5 432,10") у всіх елементах інтерфейсу.
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        // Підказки показуємо швидко і тримаємо довго, щоб користувач устиг прочитати.
        ToolTipService.InitialShowDelayProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(350));
        ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(60_000));
        ToolTipService.BetweenShowDelayProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(100));

        // Потрібно для читання старих текстових файлів у кодуванні Windows-1251.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Сталася непередбачена помилка:\n\n" + args.Exception.Message +
                "\n\nПрограма продовжить роботу. Якщо помилка повторюється — зверніться до розробника.",
                "GeoArea — помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
