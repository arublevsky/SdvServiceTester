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

    public static async Task Run(ImportSettings settings, CancellationToken token)
    {
        using var client = new RestClient(settings.ApiBase);

        Console.WriteLine($"Starting import to the server: {settings.ApiBase}");

        var totalRequests = settings.TotalRecords / settings.BatchSize;
        var models = GenerateModels(settings, totalRequests);

        var sw = Stopwatch.StartNew();
        if (settings.RequestsParallelism > 1)
        {
            await SendInParallel(settings, models, client, token);
        }
        else
        {
            await SendInSequence(settings, models, client, token);
        }

        sw.Stop();
        Console.WriteLine($"All requests sent. Time elapsed: {sw.Elapsed.TotalSeconds:0.##}s");
    }

    private static IList<string> GenerateModels(ImportSettings settings, long totalRequests)
    {
        var models = new List<string>();
        var totalMbToSend = 0d;

        for (var i = 0; i < totalRequests; i++)
        {
            var model = CreateImportModel(i, totalRequests, settings.BatchSize);
            totalMbToSend += GetBodySizeInMb(model);
            models.Add(model);
        }

        Console.WriteLine("Models prepared. " +
                          $"Total models: {models.Count}; Each with {settings.BatchSize} records; " +
                          $"Total size: {totalMbToSend:0.##} MB");
        return models;
    }

    private static Task SendInParallel(
        ImportSettings settings,
        IEnumerable<string> models,
        RestClient client,
        CancellationToken token)
    {
        Console.WriteLine($"RequestsParallelism = {settings.RequestsParallelism}, sending models in parallel.");

        return Parallel.ForEachAsync(
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
                Console.WriteLine($"Request sent. Status code: {response.StatusCode}.");
            });
    }

    private static async Task SendInSequence(
        ImportSettings settings,
        IList<string> models,
        RestClient client,
        CancellationToken token)
    {
        Console.WriteLine("RequestsParallelism = 1, sending models one-by-one.");

        for (var i = 0; i < models.Count; i++)
        {
            var request = CreateRequest(settings);
            request.AddBody(models[i]);
            var response = await client.ExecuteAsync(request, token);
            Console.WriteLine($"Request sent. Status code: {response.StatusCode}. (#{i + 1})");
        }
    }

    private static double GetBodySizeInMb(string model) => (double)Encoding.UTF8.GetByteCount(model) / (1024 * 1024);

    private static RestRequest CreateRequest(ImportSettings settings)
    {
        var request = new RestRequest(new Uri("/annals/users/recommendations", UriKind.Relative), Method.Patch);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", $"Token token=\"{settings.AuthToken}\"");
        return request;
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