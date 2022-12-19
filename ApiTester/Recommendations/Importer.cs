using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ApiTester.Recommendations.Models;
using RestSharp;

namespace ApiTester.Recommendations;

public static class Importer
{
    private const long UserIdTemplate = 88_000_000_000;
    private const long RecommendedIdTemplate = 77_00_000_000;

    private static int requestsCounter = 0;

    public static async Task Run(ImportSettings settings, CancellationToken token)
    {
        using var client = new RestClient(settings.ApiBase);

        Console.WriteLine($"Starting import to the server: {settings.ApiBase}");

        await RunImport(client, settings, token);
    }

    private static async Task RunImport(RestClient client, ImportSettings settings, CancellationToken token)
    {
        var secondsElapsed = settings.RequestsParallelism > 1
            ? await Execute(() => SendInParallel(settings, client, token))
            : await Execute(() => SendInSequence(settings, client, token));

        Console.WriteLine($"All requests sent. Time elapsed: {secondsElapsed:0.##}s");
    }

    private static async Task SendInParallel(ImportSettings settings, RestClient client, CancellationToken token)
    {
        Console.WriteLine($"RequestsParallelism = {settings.RequestsParallelism}, sending models in parallel.");
        var models = GenerateModels(settings);
        await Parallel.ForEachAsync(
            models,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.RequestsParallelism,
                CancellationToken = token
            },
            async (model, ct) =>
            {
                var request = CreateRequest(settings);
                request.AddBody(model);
                var response = await client.ExecuteAsync(request, ct);
                Interlocked.Increment(ref requestsCounter);
                Console.WriteLine($"Request sent. Status code: {response.StatusCode}. (#{requestsCounter})");
            });
    }

    private static async Task SendInSequence(
        ImportSettings settings,
        RestClient client,
        CancellationToken token)
    {
        Console.WriteLine("RequestsParallelism = 1, sending models one-by-one.");

        var models = GenerateModels(settings);

        for (var i = 0; i < models.Count; i++)
        {
            var request = CreateRequest(settings);
            request.AddBody(models[i]);
            var response = await client.ExecuteAsync(request, token);
            Console.WriteLine($"Request sent. Status code: {response.StatusCode}. (#{++i})");
        }
    }

    private static IList<string> GenerateModels(ImportSettings settings)
    {
        Console.WriteLine("Starting models generation... ");
        var models = new List<string>();
        var totalMbToSend = 0d;

        for (var i = 0; i < settings.TotalRequests; i++)
        {
            var model = CreateImportModel(i, settings.TotalRequests, settings.BatchSize);
            totalMbToSend += GetBodySizeInMb(model);
            models.Add(model);
        }

        Console.WriteLine("Models prepared. " +
                          $"Total models: {models.Count}; Each with {settings.BatchSize} records; " +
                          $"Total size: {totalMbToSend:0.##} MB");
        return models;
    }

    private static double GetBodySizeInMb(string model) => (double)Encoding.UTF8.GetByteCount(model) / (1024 * 1024);

    private static RestRequest CreateRequest(ImportSettings settings)
    {
        var request = new RestRequest(new Uri("/annals/users/recommendations", UriKind.Relative), Method.Patch);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", $"Token token=\"{settings.AuthToken}\"");
        return request;
    }

    private static async Task<double> Execute(Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        await action();
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }

    private static string CreateImportModel(long setId, long setsTotal, int totalItems)
    {
        var model = CreateEmptyModel(setId, setsTotal);
        FillData(setId, totalItems, model);
        return JsonSerializer.Serialize(model);
    }

    private static void FillData(long setId, int totalItems, Model model)
    {
        for (int i = 1; i < totalItems + 1; i++)
        {
            model.Recommendations.Data.Add(new Data
            {
                UserId = UserIdTemplate + (setId * totalItems) + i,
                Records = new[]
                {
                    new Record
                    {
                        S = i,
                        RId = RecommendedIdTemplate + (setId * i) + i
                    }
                }
            });
        }
    }

    private static Model CreateEmptyModel(long setId, long setsTotal)
    {
        return new Model
        {
            SetId = setId,
            SetsTotal = setsTotal,
            Updated = DateTime.UtcNow,
            Recommendations = new Models.Recommendations
            {
                Expiration = DateTime.UtcNow
            }
        };
    }
}