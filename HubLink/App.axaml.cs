using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HubLink.Services;
using HubLink.ViewModels;
using HubLink.Views;

namespace HubLink;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Composition root: the only place where concrete services are chosen.
            var settings = AppSettings.Load();
            var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan }; // timeout handled per request
            var service = new ShipmentApiClient(http, settings);
            var vm = new MainViewModel(service, new CsvExporter(), settings);
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
