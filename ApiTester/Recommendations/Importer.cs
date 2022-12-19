using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using ApiTester.Recommendations.Models;
using RestSharp;

namespace ApiTester.Recommendations;

public static class Importer
{
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
        Console.WriteLine($"RequestsParallelism = {settings.RequestsParallelism}, sending {settings.TotalRequests} models in parallel.");
        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(1, settings.TotalRequests + 1),
                GetParallelOptions(settings, token),
                async (i, ct) =>
                {
                    var model = CreateImportModel(i, settings);
                    await SendRequest(settings, client, model, i, ct);
                });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Process cancelled, stopping...");
        }
    }

    private static ParallelOptions GetParallelOptions(ImportSettings settings, CancellationToken token)
    {
        return new ParallelOptions
        {
            MaxDegreeOfParallelism = settings.RequestsParallelism,
            CancellationToken = token
        };
    }

    private static async Task SendInSequence(
        ImportSettings settings,
        RestClient client,
        CancellationToken token)
    {
        Console.WriteLine("RequestsParallelism = 1, sending models one-by-one.");

        var models = GenerateAllModels(settings);

        for (var i = 0; i < models.Count; i++)
        {
            await SendRequest(settings, client, models[i], i, token);
        }
    }

    private static async Task SendRequest(
        ImportSettings settings,
        RestClient client,
        string model,
        int index,
        CancellationToken token)
    {
        var request = CreateRequest(settings, model);
        var response = await client.ExecuteAsync(request, token);
        Console.WriteLine($"Request sent. Status code: {response.StatusCode}. (#{index})");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Non success status code: [{response.Content}] [{response.ErrorMessage}]");
        }
    }

    private static IList<string> GenerateAllModels(ImportSettings settings)
    {
        Console.WriteLine("Starting models generation... ");
        var models = new List<string>();
        var totalMbToSend = 0d;

        for (var i = 0; i < settings.TotalRequests; i++)
        {
            var model = CreateImportModel(i, settings);
            totalMbToSend += GetBodySizeInMb(model);
            models.Add(model);
        }

        Console.WriteLine("Models prepared. " +
                          $"Total models: {models.Count}; Each with {settings.BatchSize} records; " +
                          $"Total size: {totalMbToSend:0.##} MB");
        return models;
    }

    private static double GetBodySizeInMb(string model) => (double)Encoding.UTF8.GetByteCount(model) / (1024 * 1024);

    private static RestRequest CreateRequest(ImportSettings settings, string model)
    {
        var request = new RestRequest(new Uri("/annals/users/recommendations", UriKind.Relative), Method.Patch);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", $"Token token=\"{settings.AuthToken}\"");
        request.AddBody(model);
        return request;
    }

    private static async Task<double> Execute(Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        await action();
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }

    private static string CreateImportModel(long setId, ImportSettings settings)
    {
        var model = CreateEmptyModel(setId, settings.TotalRequests);
        FillData(setId, settings, model);
        return JsonSerializer.Serialize(model);
    }

    private static void FillData(long setId, ImportSettings settings, Model model)
    {
        var i = 1;
        while (i < settings.BatchSize + 1)
        {
            var data = new Data
            {
                UserId = settings.UserIdTemplate + (setId * settings.BatchSize) + i,
                Records = new List<Record>(),
            };

            for (var j = 1; j < settings.RecommendationsPerUser + 1; j++)
            {
                i++;
                data.Records.Add(new Record
                {
                    S = Random.Shared.Next(1, 100),
                    RId = settings.RecommendedIdTemplate + (setId * settings.BatchSize) + i + j
                });
            }

            model.Recommendations.Data.Add(data);
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