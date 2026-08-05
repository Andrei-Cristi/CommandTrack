using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommandTrack.Shared.Telemetry;
using CommandTrack.Shared.Units;

string apiBaseUrl = LoadApiBaseUrl();

Guid unitId = Guid.Parse(
    "89da676e-384e-4bb0-adec-1d4bfac22c8b");

double batteryPercent = 95.0;
double latitude = 44.4268;
double longitude = 26.1025;

using HttpClient httpClient = new()
{
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromSeconds(10)
};

using CancellationTokenSource cancellationTokenSource = new();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

Console.WriteLine("CommandTrack Platform Simulator");
Console.WriteLine($"Unit ID: {unitId}");
Console.WriteLine($"API: {apiBaseUrl}");
Console.WriteLine("Sending heartbeat and telemetry every 5 seconds.");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

while (!cancellationTokenSource.Token.IsCancellationRequested)
{
    try
    {
        using HttpResponseMessage heartbeatResponse =
            await httpClient.PostAsync(
                $"api/units/{unitId}/heartbeat",
                content: null,
                cancellationTokenSource.Token);

        if (heartbeatResponse.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine(
                $"[{DateTimeOffset.Now:HH:mm:ss}] " +
                $"Unit '{unitId}' was not found.");

            break;
        }

        heartbeatResponse.EnsureSuccessStatusCode();

        UnitDto? unit =
            await heartbeatResponse.Content
                .ReadFromJsonAsync<UnitDto>(
                    cancellationToken:
                        cancellationTokenSource.Token);

        batteryPercent = Math.Max(
            0,
            batteryPercent -
            Random.Shared.NextDouble() * 0.25);

        latitude +=
            (Random.Shared.NextDouble() - 0.5) * 0.0002;

        longitude +=
            (Random.Shared.NextDouble() - 0.5) * 0.0002;

        double speedKph =
            8 + Random.Shared.NextDouble() * 15;

        CreateUnitTelemetryRequest telemetryRequest = new(
            BatteryPercent: Math.Round(batteryPercent, 2),
            Latitude: Math.Round(latitude, 6),
            Longitude: Math.Round(longitude, 6),
            SpeedKph: Math.Round(speedKph, 2),
            RecordedAtUtc: DateTimeOffset.UtcNow);

        Console.WriteLine(
    $"REQUEST => Battery: {telemetryRequest.BatteryPercent:F2}% | " +
    $"Position: {telemetryRequest.Latitude:F6}, " +
    $"{telemetryRequest.Longitude:F6} | " +
    $"Speed: {telemetryRequest.SpeedKph:F2} km/h");

        using HttpResponseMessage telemetryResponse =
            await httpClient.PostAsJsonAsync(   
                $"api/units/{unitId}/telemetry",
                telemetryRequest,
                cancellationTokenSource.Token);

        telemetryResponse.EnsureSuccessStatusCode();

        UnitTelemetryDto? telemetry =
            await telemetryResponse.Content
                .ReadFromJsonAsync<UnitTelemetryDto>(
                    cancellationToken:
                        cancellationTokenSource.Token);

        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] " +
            $"Heartbeat sent | " +
            $"{unit?.CallSign} | " +
            $"Status: {unit?.Status}");

        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] " +
            $"Telemetry sent | " +
            $"Battery: {telemetry?.BatteryPercent:F2}% | " +
            $"Position: {telemetry?.Latitude:F6}, " +
            $"{telemetry?.Longitude:F6} | " +
            $"Speed: {telemetry?.SpeedKph:F2} km/h");

        Console.WriteLine();
    }
    catch (HttpRequestException exception)
    {
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] " +
            $"API communication failed: {exception.Message}");
    }
    catch (TaskCanceledException)
        when (!cancellationTokenSource.IsCancellationRequested)
    {
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] " +
            "The API request timed out.");
    }
    catch (OperationCanceledException)
        when (cancellationTokenSource.IsCancellationRequested)
    {
        break;
    }

    try
    {
        await Task.Delay(
            TimeSpan.FromSeconds(5),
            cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine();
Console.WriteLine("Platform Simulator stopped.");

static string LoadApiBaseUrl()
{
    string configurationPath = Path.Combine(
        AppContext.BaseDirectory,
        "appsettings.json");

    using FileStream configurationFile =
        File.OpenRead(configurationPath);

    using JsonDocument configuration =
        JsonDocument.Parse(configurationFile);

    string? configuredUrl = configuration.RootElement
        .GetProperty("Api")
        .GetProperty("BaseUrl")
        .GetString();

    if (!Uri.TryCreate(
            configuredUrl,
            UriKind.Absolute,
            out Uri? apiBaseUri) ||
        (apiBaseUri.Scheme != Uri.UriSchemeHttp &&
         apiBaseUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidDataException(
            "Api:BaseUrl must be a valid HTTP or HTTPS URL.");
    }

    return apiBaseUri.AbsoluteUri;
}
