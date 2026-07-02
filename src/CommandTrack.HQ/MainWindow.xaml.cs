using CommandTrack.Shared.Telemetry;
using CommandTrack.Shared.Units;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;

namespace CommandTrack.HQ;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };

    private const string ApiBaseUrl =
        "http://localhost:5076/";

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly ObservableCollection<UnitDashboardRow> _units = new();

    private bool _isLoading;

    public MainWindow()
    {
        InitializeComponent();

        UnitsDataGrid.ItemsSource = _units;
        StatusComboBox.ItemsSource = new[]
{
    "Offline",
    "Online",
    "Busy",
    "Maintenance"
};

        Loaded += MainWindow_Loaded;
        RefreshButton.Click += RefreshButton_Click;
        UnitsDataGrid.SelectionChanged +=
    UnitsDataGrid_SelectionChanged;

        UpdateStatusButton.Click +=
            UpdateStatusButton_Click;

        _refreshTimer.Tick += RefreshTimer_Tick;

        Closed += (_, _) =>
        {
            _refreshTimer.Stop();
            _httpClient.Dispose();
        };
    }

    private async void UpdateStatusButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (UnitsDataGrid.SelectedItem
                is not UnitDashboardRow selectedUnit ||
            StatusComboBox.SelectedItem
                is not string selectedStatus)
        {
            StatusUpdateMessageTextBlock.Text =
                "Select a unit and a status.";

            return;
        }

        if (string.Equals(
                selectedUnit.Status,
                selectedStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            StatusUpdateMessageTextBlock.Text =
                $"The unit is already {selectedStatus}.";

            return;
        }

        UpdateStatusButton.IsEnabled = false;
        StatusComboBox.IsEnabled = false;

        StatusUpdateMessageTextBlock.Text =
            "Updating unit status...";

        try
        {
            UpdateUnitStatusRequest request =
                new(selectedStatus);

            using HttpResponseMessage response =
                await _httpClient.PatchAsJsonAsync(
                    $"api/units/{selectedUnit.Id}/status",
                    request);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync();

                StatusUpdateMessageTextBlock.Text =
                    $"Update failed: {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}. {error}";

                return;
            }

            await LoadUnitsAsync();

            StatusUpdateMessageTextBlock.Text =
                $"Status updated to {selectedStatus}.";
        }
        catch (HttpRequestException exception)
        {
            StatusUpdateMessageTextBlock.Text =
                $"Could not connect to the API: {exception.Message}";
        }
        catch (TaskCanceledException)
        {
            StatusUpdateMessageTextBlock.Text =
                "The status update request timed out.";
        }
        catch (Exception exception)
        {
            StatusUpdateMessageTextBlock.Text =
                $"Unexpected error: {exception.Message}";
        }
        finally
        {
            bool hasSelection =
                UnitsDataGrid.SelectedItem is UnitDashboardRow;

            StatusComboBox.IsEnabled = hasSelection;
            UpdateStatusButton.IsEnabled = hasSelection;
        }
    }

    private async void MainWindow_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        await LoadUnitsAsync();

        _refreshTimer.Start();
    }

    private async void RefreshTimer_Tick(
    object? sender,
    EventArgs e)
    {
        await LoadUnitsAsync();
    }

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadUnitsAsync();
    }

    private void UnitsDataGrid_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (UnitsDataGrid.SelectedItem
            is not UnitDashboardRow selectedUnit)
        {
            StatusComboBox.SelectedItem = null;
            StatusComboBox.IsEnabled = false;
            UpdateStatusButton.IsEnabled = false;
            StatusUpdateMessageTextBlock.Text = string.Empty;

            return;
        }

        StatusComboBox.IsEnabled = true;
        UpdateStatusButton.IsEnabled = true;
        StatusComboBox.SelectedItem = selectedUnit.Status;
        StatusUpdateMessageTextBlock.Text = string.Empty;
    }

    private async Task LoadUnitsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Guid? selectedUnitId =
    (UnitsDataGrid.SelectedItem as UnitDashboardRow)?.Id;
        RefreshButton.IsEnabled = false;
        StatusTextBlock.Text = "Loading operational units...";

        try
        {
            List<UnitDto>? units =
                await _httpClient.GetFromJsonAsync<List<UnitDto>>(
                    "api/units");

            units ??= new List<UnitDto>();

            List<UnitDashboardRow> rows = new();

            foreach (UnitDto unit in units)
            {
                UnitTelemetryDto? telemetry =
                    await GetLatestTelemetryAsync(unit.Id);

                rows.Add(
                    new UnitDashboardRow(
                        unit,
                        telemetry));
            }

            _units.Clear();

            foreach (UnitDashboardRow row in rows)
            {
                _units.Add(row);
            }
            
            if (selectedUnitId.HasValue)
            {
                UnitsDataGrid.SelectedItem =
                    _units.FirstOrDefault(
                        unit => unit.Id == selectedUnitId.Value);
            }

            int onlineCount =
                units.Count(unit =>
                    string.Equals(
                        unit.Status,
                        "Online",
                        StringComparison.OrdinalIgnoreCase));

            int offlineCount =
                units.Count(unit =>
                    string.Equals(
                        unit.Status,
                        "Offline",
                        StringComparison.OrdinalIgnoreCase));

            TotalUnitsTextBlock.Text =
                units.Count.ToString();

            OnlineUnitsTextBlock.Text =
                onlineCount.ToString();

            OfflineUnitsTextBlock.Text =
                offlineCount.ToString();

            LastRefreshTextBlock.Text =
                DateTimeOffset.Now.ToString("HH:mm:ss");

            StatusTextBlock.Text =
                $"Loaded {units.Count} operational units.";
        }
        catch (HttpRequestException exception)
        {
            StatusTextBlock.Text =
                $"Could not connect to the API: {exception.Message}";
        }
        catch (TaskCanceledException)
        {
            StatusTextBlock.Text =
                "The API request timed out.";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text =
                $"Unexpected error: {exception.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            _isLoading = false;
        }
    }

    private async Task<UnitTelemetryDto?> GetLatestTelemetryAsync(
        Guid unitId)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                $"api/units/{unitId}/telemetry/latest");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<UnitTelemetryDto>();
    }
}

public sealed class UnitDashboardRow
{
    public UnitDashboardRow(
        UnitDto unit,
        UnitTelemetryDto? telemetry)
    {
        Id = unit.Id;
        CallSign = unit.CallSign;
        Type = unit.Type;
        Status = unit.Status;
        CreatedAtUtc = unit.CreatedAtUtc;
        LastSeenAtUtc = unit.LastSeenAtUtc;

        BatteryPercent = telemetry?.BatteryPercent;
        SpeedKph = telemetry?.SpeedKph;
        Latitude = telemetry?.Latitude;
        Longitude = telemetry?.Longitude;
        TelemetryRecordedAtUtc = telemetry?.RecordedAtUtc;
    }

    public Guid Id { get; }

    public string CallSign { get; }

    public string Type { get; }

    public string Status { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? LastSeenAtUtc { get; }

    public double? BatteryPercent { get; }

    public double? SpeedKph { get; }

    public double? Latitude { get; }

    public double? Longitude { get; }

    public DateTimeOffset? TelemetryRecordedAtUtc { get; }

    public string IdDisplay =>
        Id.ToString();

    public string BatteryDisplay =>
        BatteryPercent.HasValue
            ? $"{BatteryPercent.Value:F1}%"
            : "—";

    public string SpeedDisplay =>
        SpeedKph.HasValue
            ? $"{SpeedKph.Value:F1} km/h"
            : "—";

    public string PositionDisplay =>
        Latitude.HasValue && Longitude.HasValue
            ? $"{Latitude.Value:F6}, {Longitude.Value:F6}"
            : "—";

    public string CreatedAtDisplay =>
        CreatedAtUtc
            .ToLocalTime()
            .ToString("dd.MM.yyyy HH:mm:ss");

    public string LastSeenDisplay =>
        LastSeenAtUtc.HasValue
            ? LastSeenAtUtc.Value
                .ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm:ss")
            : "Never";

    public string TelemetryRecordedAtDisplay =>
        TelemetryRecordedAtUtc.HasValue
            ? TelemetryRecordedAtUtc.Value
                .ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm:ss")
            : "No telemetry";
}