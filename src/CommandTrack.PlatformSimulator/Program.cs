using System.Net;
using System.Net.Http.Json;
using CommandTrack.Shared.Units;

const string apiBaseUrl = "http://localhost:5076/";

Guid unitId = Guid.Parse(
    "89da676e-384e-4bb0-adec-1d4bfac22c8b");

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
Console.WriteLine("Sending a heartbeat every 5 seconds.");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

while (!cancellationTokenSource.Token.IsCancellationRequested)
{
    try
    {
        using HttpResponseMessage response =
            await httpClient.PostAsync(
                $"api/units/{unitId}/heartbeat",
                content: null,
                cancellationTokenSource.Token);

        if (response.IsSuccessStatusCode)
        {
            UnitDto? unit =
                await response.Content.ReadFromJsonAsync<UnitDto>(
                    cancellationToken:
                        cancellationTokenSource.Token);

            Console.WriteLine(
                $"[{DateTimeOffset.Now:HH:mm:ss}] " +
                $"Heartbeat sent | " +
                $"{unit?.CallSign} | " +
                $"Status: {unit?.Status} | " +
                $"Last seen: {unit?.LastSeenAtUtc:O}");
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine(
                $"[{DateTimeOffset.Now:HH:mm:ss}] " +
                $"Unit '{unitId}' was not found.");
        }
        else
        {
            string errorContent =
                await response.Content.ReadAsStringAsync(
                    cancellationTokenSource.Token);

            Console.WriteLine(
                $"[{DateTimeOffset.Now:HH:mm:ss}] " +
                $"Heartbeat failed: " +
                $"{(int)response.StatusCode} " +
                $"{response.ReasonPhrase}");

            Console.WriteLine(errorContent);
        }
    }
    catch (HttpRequestException exception)
    {
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] " +
            $"Could not connect to the API: " +
            exception.Message);
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