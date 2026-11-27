using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HubLink.ViewModels;

namespace HubLink.Views;

// Code-behind contains only view concerns: the OS file-save dialog and the initial load trigger.
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PickSaveFilePath = PickSavePathAsync;
                await vm.RefreshAsync();
            }
        };
    }

    private async Task<string?> PickSavePathAsync(string suggestedName)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export shipments to CSV",
            SuggestedFileName = suggestedName,
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV file") { Patterns = new[] { "*.csv" } } }
        });
        return file?.TryGetLocalPath();
    }
}
