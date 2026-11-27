using System.Collections.ObjectModel;
using System.Globalization;
using HubLink.Models;
using HubLink.Services;

namespace HubLink.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string All = "All";
    private readonly IShipmentService _service;
    private readonly ICsvExporter _csv;
    private readonly AppSettings _settings;

    private List<Shipment> _all = new();        // everything from the last successful retrieval
    private List<Shipment> _displayed = new();  // what the list currently shows (= what is exported)
    private bool _hasLoaded;
    private DateTime _lastRetrieved;

    public MainViewModel(IShipmentService service, ICsvExporter csv, AppSettings settings)
    {
        _service = service;
        _csv = csv;
        _settings = settings;
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsLoading);
        ExportCommand = new AsyncCommand(ExportAsync, () => !IsLoading);
        FooterText = settings.LoadWarning ?? "Ready.";
    }

    /// <summary>Set by the view: asks the user where to save (returns null if cancelled).</summary>
    public Func<string, Task<string?>>? PickSaveFilePath { get; set; }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand ExportCommand { get; }

    public IReadOnlyList<string> StatusFilters { get; } = new[] { All, "Pending", "InTransit", "Delivered", "Exception" };
    public ObservableCollection<Shipment> Shipments { get; } = new();

    public string ServiceInfo => $"Branch: {_settings.BranchCode}    |    Request: {_service.RequestDescription}";

    // ---- state ----
    private string _selectedFilter = All;
    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value ?? All))
            {
                ExportSuccessMessage = null;
                ExportErrorMessage = null;
                ApplyFilter();
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                ExportCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _lastFailed;
    public string RefreshButtonText => IsLoading ? "Loading…" : (_lastFailed ? "Retry" : "Refresh");

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) Raise(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    private string? _infoMessage;
    public string? InfoMessage { get => _infoMessage; private set { if (SetProperty(ref _infoMessage, value)) Raise(nameof(HasInfo)); } }
    public bool HasInfo => !string.IsNullOrEmpty(_infoMessage);

    private string? _exportSuccess;
    public string? ExportSuccessMessage { get => _exportSuccess; private set { if (SetProperty(ref _exportSuccess, value)) Raise(nameof(HasExportSuccess)); } }
    public bool HasExportSuccess => !string.IsNullOrEmpty(_exportSuccess);

    private string? _exportError;
    public string? ExportErrorMessage { get => _exportError; private set { if (SetProperty(ref _exportError, value)) Raise(nameof(HasExportError)); } }
    public bool HasExportError => !string.IsNullOrEmpty(_exportError);

    private bool _isStale;
    public bool IsStale
    {
        get => _isStale;
        private set { if (SetProperty(ref _isStale, value)) { Raise(nameof(StaleMessage)); Raise(nameof(SummaryTitle)); } }
    }
    public string StaleMessage =>
        $"Showing PREVIOUSLY LOADED data from {_lastRetrieved:HH:mm:ss}. The latest refresh failed, so this information may be out of date.";

    private string _footer = "";
    public string FooterText { get => _footer; private set => SetProperty(ref _footer, value); }

    // ---- summary (always based on ALL retrieved shipments, not the filtered list) ----
    private int _total, _pending, _inTransit, _delivered, _exception;
    public int TotalCount { get => _total; private set => SetProperty(ref _total, value); }
    public int PendingCount { get => _pending; private set => SetProperty(ref _pending, value); }
    public int InTransitCount { get => _inTransit; private set => SetProperty(ref _inTransit, value); }
    public int DeliveredCount { get => _delivered; private set => SetProperty(ref _delivered, value); }
    public int ExceptionCount { get => _exception; private set => SetProperty(ref _exception, value); }
    public string SummaryTitle => IsStale
        ? "Summary of all shipments (previously loaded data)"
        : "Summary of all retrieved shipments (unaffected by the filter)";

    public string ListCaption => _hasLoaded ? $"Showing {Shipments.Count} of {_all.Count} shipment(s)" : "No data loaded yet";

    public string EmptyMessage
    {
        get
        {
            if (!_hasLoaded || Shipments.Count > 0) return "";
            return _all.Count == 0
                ? $"The service responded successfully, but returned no shipments for branch {_settings.BranchCode}."
                : $"No shipments match the \"{_selectedFilter}\" status filter.";
        }
    }
    public bool HasEmptyMessage => EmptyMessage.Length > 0;

    // ---- retrieval ----
    public async Task RefreshAsync()
    {
        if (IsLoading) return;               // never run overlapping requests
        IsLoading = true;
        Raise(nameof(RefreshButtonText));
        ErrorMessage = null;
        InfoMessage = null;
        ExportSuccessMessage = null;
        ExportErrorMessage = null;
        FooterText = "Retrieving shipments…";
        try
        {
            var result = await _service.GetShipmentsAsync();
            _all = result.Shipments.ToList();
            _hasLoaded = true;
            _lastFailed = false;
            _lastRetrieved = DateTime.Now;
            IsStale = false;
            UpdateSummary();
            ApplyFilter();
            InfoMessage = _all.Count == 0
                ? $"Refresh succeeded: the service returned no shipments for branch {result.BranchCode}."
                : $"Refresh succeeded: {_all.Count} shipment(s) retrieved for branch {result.BranchCode}.";
            FooterText = $"Last successful retrieval: {_lastRetrieved:yyyy-MM-dd HH:mm:ss}";
        }
        catch (ShipmentServiceException ex)
        {
            Fail(ex.Message);
        }
        catch (Exception ex)
        {
            Fail($"An unexpected error occurred while retrieving shipments: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            Raise(nameof(RefreshButtonText));
        }
    }

    private void Fail(string message)
    {
        _lastFailed = true;
        ErrorMessage = message + " Press Retry to try again.";
        IsStale = _hasLoaded;   // old data (if any) stays visible but is labelled as previously loaded
        FooterText = _hasLoaded
            ? $"Refresh failed. Showing data from {_lastRetrieved:yyyy-MM-dd HH:mm:ss}."
            : "Refresh failed. No data has been loaded.";
    }

    private void UpdateSummary()
    {
        TotalCount = _all.Count;
        PendingCount = _all.Count(s => s.Status == ShipmentStatus.Pending);
        InTransitCount = _all.Count(s => s.Status == ShipmentStatus.InTransit);
        DeliveredCount = _all.Count(s => s.Status == ShipmentStatus.Delivered);
        ExceptionCount = _all.Count(s => s.Status == ShipmentStatus.Exception);
    }

    private void ApplyFilter()
    {
        _displayed = _all.Where(s => _selectedFilter == All || s.StatusText == _selectedFilter).ToList();
        Shipments.Clear();
        foreach (var s in _displayed) Shipments.Add(s);
        Raise(nameof(ListCaption));
        Raise(nameof(EmptyMessage));
        Raise(nameof(HasEmptyMessage));
    }

    // ---- export ----
    public async Task ExportAsync()
    {
        if (IsLoading) return;
        ExportSuccessMessage = null;
        ExportErrorMessage = null;

        var rows = _displayed.ToList();   // snapshot of exactly what is displayed
        if (rows.Count == 0)
        {
            ExportErrorMessage = "Nothing to export: no shipments are currently displayed. No file was created.";
            return;
        }

        try
        {
            var suggested = $"shipments-{_settings.BranchCode}-{_selectedFilter}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var path = PickSaveFilePath == null ? null : await PickSaveFilePath(suggested);
            if (string.IsNullOrWhiteSpace(path))
            {
                FooterText = "Export cancelled. No file was created.";
                return;
            }

            await _csv.WriteAsync(path, rows);
            ExportSuccessMessage = $"Export succeeded: {rows.Count} record(s) (filter: {_selectedFilter}) saved to {path}" +
                                   (IsStale ? " — note: this data is from a previous retrieval." : ".");
        }
        catch (Exception ex)
        {
            ExportErrorMessage = $"Export failed: {ex.Message} The file was not saved.";
        }
    }
}
