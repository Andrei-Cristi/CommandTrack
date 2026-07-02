using CommandTrack.Shared.Telemetry;
using CommandTrack.Shared.Units;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CommandTrack.HQ;

public partial class MainWindow : Window
{
    private const string ApiBaseUrl =
        "http://localhost:5076/";

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly ObservableCollection<UnitDashboardRow> _units = new();

    private readonly DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };

    private Guid? _loadedHistoryUnitId;

    private List<UnitTelemetryDto> _telemetryHistory = new();

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

        BatteryChartCanvas.SizeChanged +=
    (_, _) => DrawTelemetryCharts();

        SpeedChartCanvas.SizeChanged +=
            (_, _) => DrawTelemetryCharts();

        Closed += (_, _) =>
        {
            _refreshTimer.Stop();
            _httpClient.Dispose();
        };
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

            StatusUpdateMessageTextBlock.Text =
                string.Empty;

            _loadedHistoryUnitId = null;
            _telemetryHistory = new List<UnitTelemetryDto>();
            TelemetryHistoryStatusTextBlock.Text =
    "Select a unit to view its history.";

            BatteryChartCanvas.Children.Clear();
            SpeedChartCanvas.Children.Clear();
            return;
        }

        StatusComboBox.IsEnabled = true;
        UpdateStatusButton.IsEnabled = true;

        StatusComboBox.SelectedItem =
            selectedUnit.Status;

        StatusUpdateMessageTextBlock.Text =
            string.Empty;

        _ = LoadSelectedUnitHistoryAsync(
            selectedUnit.Id);
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
                $"Could not connect to the API: " +
                exception.Message;
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
                UnitsDataGrid.SelectedItem
                is UnitDashboardRow;

            StatusComboBox.IsEnabled =
                hasSelection;

            UpdateStatusButton.IsEnabled =
                hasSelection;
        }
    }

    private async Task LoadUnitsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        Guid? selectedUnitId =
            (UnitsDataGrid.SelectedItem
                as UnitDashboardRow)?.Id;

        RefreshButton.IsEnabled = false;

        StatusTextBlock.Text =
            "Loading operational units...";

        try
        {
            List<UnitDto>? units =
                await _httpClient
                    .GetFromJsonAsync<List<UnitDto>>(
                        "api/units");

            units ??= new List<UnitDto>();

            List<UnitDashboardRow> rows = new();

            foreach (UnitDto unit in units)
            {
                UnitTelemetryDto? telemetry =
                    await GetLatestTelemetryAsync(
                        unit.Id);

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
                        unit =>
                            unit.Id ==
                            selectedUnitId.Value);
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

            int busyCount =
                units.Count(unit =>
                    string.Equals(
                        unit.Status,
                        "Busy",
                        StringComparison.OrdinalIgnoreCase));

            int maintenanceCount =
                units.Count(unit =>
                    string.Equals(
                        unit.Status,
                        "Maintenance",
                        StringComparison.OrdinalIgnoreCase));

            TotalUnitsTextBlock.Text =
                units.Count.ToString();

            OnlineUnitsTextBlock.Text =
                onlineCount.ToString();

            OfflineUnitsTextBlock.Text =
                offlineCount.ToString();

            BusyUnitsTextBlock.Text =
                busyCount.ToString();

            MaintenanceUnitsTextBlock.Text =
                maintenanceCount.ToString();

            LastRefreshTextBlock.Text =
                DateTimeOffset.Now.ToString("HH:mm:ss");

            StatusTextBlock.Text =
                $"Loaded {units.Count} operational units.";
        }
        catch (HttpRequestException exception)
        {
            StatusTextBlock.Text =
                $"Could not connect to the API: " +
                exception.Message;
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

    private async Task<UnitTelemetryDto?>
        GetLatestTelemetryAsync(
            Guid unitId)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                $"api/units/{unitId}/telemetry/latest");

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<UnitTelemetryDto>();
    }

    private async Task<List<UnitTelemetryDto>>
        GetTelemetryHistoryAsync(
            Guid unitId,
            int limit = 30)
    {
        List<UnitTelemetryDto>? history =
            await _httpClient
                .GetFromJsonAsync<List<UnitTelemetryDto>>(
                    $"api/units/{unitId}/telemetry/history" +
                    $"?limit={limit}");

        return history ??
            new List<UnitTelemetryDto>();
    }

    private async Task LoadSelectedUnitHistoryAsync(
    Guid unitId)
    {
        try
        {
            List<UnitTelemetryDto> history =
                await GetTelemetryHistoryAsync(
                    unitId);

            if (UnitsDataGrid.SelectedItem
                    is not UnitDashboardRow selectedUnit ||
                selectedUnit.Id != unitId)
            {
                return;
            }

            _telemetryHistory = history;
            _loadedHistoryUnitId = unitId;

            TelemetryHistoryStatusTextBlock.Text =
                history.Count == 0
                    ? "No telemetry history available."
                    : $"Showing the latest {history.Count} measurements.";

            DrawTelemetryCharts();
        }
        catch (HttpRequestException exception)
        {
            TelemetryHistoryStatusTextBlock.Text =
                $"Could not load history: {exception.Message}";

            BatteryChartCanvas.Children.Clear();
            SpeedChartCanvas.Children.Clear();
        }
        catch (TaskCanceledException)
        {
            TelemetryHistoryStatusTextBlock.Text =
                "The telemetry history request timed out.";

            BatteryChartCanvas.Children.Clear();
            SpeedChartCanvas.Children.Clear();
        }
        catch (Exception exception)
        {
            TelemetryHistoryStatusTextBlock.Text =
                $"Could not load history: {exception.Message}";

            BatteryChartCanvas.Children.Clear();
            SpeedChartCanvas.Children.Clear();
        }
    }

    private void DrawTelemetryCharts()
    {
        if (!_loadedHistoryUnitId.HasValue ||
            _telemetryHistory.Count == 0)
        {
            DrawChartMessage(
                BatteryChartCanvas,
                "No battery history.");

            DrawChartMessage(
                SpeedChartCanvas,
                "No speed history.");

            return;
        }

        DrawLineChart(
            BatteryChartCanvas,
            _telemetryHistory,
            telemetry => telemetry.BatteryPercent,
            minimumValue: 0,
            maximumValue: 100,
            lineBrush: new SolidColorBrush(
                Color.FromRgb(75, 213, 138)),
            valueSuffix: "%");

        double highestSpeed =
            _telemetryHistory.Max(
                telemetry => telemetry.SpeedKph);

        double maximumSpeed =
            Math.Max(
                10,
                Math.Ceiling(highestSpeed / 10) * 10);

        DrawLineChart(
            SpeedChartCanvas,
            _telemetryHistory,
            telemetry => telemetry.SpeedKph,
            minimumValue: 0,
            maximumValue: maximumSpeed,
            lineBrush: new SolidColorBrush(
                Color.FromRgb(40, 120, 212)),
            valueSuffix: " km/h");
    }

    private static void DrawLineChart(
        Canvas canvas,
        IReadOnlyList<UnitTelemetryDto> history,
        Func<UnitTelemetryDto, double> valueSelector,
        double minimumValue,
        double maximumValue,
        Brush lineBrush,
        string valueSuffix)
    {
        canvas.Children.Clear();

        double width = canvas.ActualWidth;
        double height = canvas.ActualHeight;

        if (width < 100 || height < 70)
        {
            return;
        }

        const double leftMargin = 46;
        const double rightMargin = 14;
        const double topMargin = 14;
        const double bottomMargin = 26;

        double plotWidth =
            width - leftMargin - rightMargin;

        double plotHeight =
            height - topMargin - bottomMargin;

        Brush gridBrush =
            new SolidColorBrush(
                Color.FromRgb(42, 52, 71));

        Brush textBrush =
            new SolidColorBrush(
                Color.FromRgb(143, 155, 173));

        for (int index = 0; index <= 2; index++)
        {
            double ratio = index / 2.0;

            double y =
                topMargin +
                plotHeight -
                ratio * plotHeight;

            Line gridLine = new()
            {
                X1 = leftMargin,
                X2 = leftMargin + plotWidth,
                Y1 = y,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1
            };

            canvas.Children.Add(gridLine);

            double axisValue =
                minimumValue +
                ratio * (maximumValue - minimumValue);

            TextBlock axisLabel = new()
            {
                Text = axisValue.ToString("F0"),
                Foreground = textBrush,
                FontSize = 10
            };

            Canvas.SetLeft(axisLabel, 2);
            Canvas.SetTop(axisLabel, y - 8);

            canvas.Children.Add(axisLabel);
        }

        Polyline series = new()
        {
            Stroke = lineBrush,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (int index = 0;
             index < history.Count;
             index++)
        {
            double x =
                history.Count == 1
                    ? leftMargin + plotWidth / 2
                    : leftMargin +
                      index * plotWidth /
                      (history.Count - 1);

            double value =
                Math.Clamp(
                    valueSelector(history[index]),
                    minimumValue,
                    maximumValue);

            double normalizedValue =
                (value - minimumValue) /
                (maximumValue - minimumValue);

            double y =
                topMargin +
                plotHeight -
                normalizedValue * plotHeight;

            series.Points.Add(
                new Point(x, y));
        }

        canvas.Children.Add(series);

        Point latestPoint =
            series.Points[^1];

        Ellipse latestMarker = new()
        {
            Width = 8,
            Height = 8,
            Fill = lineBrush,
            Stroke = Brushes.White,
            StrokeThickness = 1
        };

        Canvas.SetLeft(
            latestMarker,
            latestPoint.X - 4);

        Canvas.SetTop(
            latestMarker,
            latestPoint.Y - 4);

        canvas.Children.Add(latestMarker);

        double latestValue =
            valueSelector(history[^1]);

        TextBlock latestValueLabel = new()
        {
            Text =
                $"{latestValue:F1}{valueSuffix}",
            Foreground = lineBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };

        Canvas.SetRight(
            latestValueLabel,
            rightMargin);

        Canvas.SetTop(
            latestValueLabel,
            0);

        canvas.Children.Add(latestValueLabel);

        TextBlock firstTimeLabel = new()
        {
            Text = history[0]
                .RecordedAtUtc
                .ToLocalTime()
                .ToString("HH:mm:ss"),
            Foreground = textBrush,
            FontSize = 10
        };

        Canvas.SetLeft(
            firstTimeLabel,
            leftMargin);

        Canvas.SetTop(
            firstTimeLabel,
            height - 18);

        canvas.Children.Add(firstTimeLabel);

        TextBlock lastTimeLabel = new()
        {
            Text = history[^1]
                .RecordedAtUtc
                .ToLocalTime()
                .ToString("HH:mm:ss"),
            Foreground = textBrush,
            FontSize = 10
        };

        Canvas.SetRight(
            lastTimeLabel,
            rightMargin);

        Canvas.SetTop(
            lastTimeLabel,
            height - 18);

        canvas.Children.Add(lastTimeLabel);
    }

    private static void DrawChartMessage(
        Canvas canvas,
        string message)
    {
        canvas.Children.Clear();

        TextBlock messageTextBlock = new()
        {
            Text = message,
            Foreground = new SolidColorBrush(
                Color.FromRgb(127, 138, 155)),
            FontSize = 12
        };

        Canvas.SetLeft(
            messageTextBlock,
            12);

        Canvas.SetTop(
            messageTextBlock,
            12);

        canvas.Children.Add(
            messageTextBlock);
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

        BatteryPercent =
            telemetry?.BatteryPercent;

        SpeedKph =
            telemetry?.SpeedKph;

        Latitude =
            telemetry?.Latitude;

        Longitude =
            telemetry?.Longitude;

        TelemetryRecordedAtUtc =
            telemetry?.RecordedAtUtc;
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
        Latitude.HasValue &&
        Longitude.HasValue
            ? $"{Latitude.Value:F6}, " +
              $"{Longitude.Value:F6}"
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