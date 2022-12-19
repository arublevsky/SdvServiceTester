namespace ApiTester.Recommendations;

public class ImportSettings
{
    public string AuthToken { get; set; }

    public string ApiBase { get; set; }

    public int RecommendationsPerUser { get; set; }

    public int TotalRecords { get; set; }

    public long UserIdTemplate { get; set; }

    public long RecommendedIdTemplate { get; set; }

    public int BatchSize { get; set; }

    public int RequestsParallelism { get; set; }

    public int TotalRequests => TotalRecords / BatchSize;
}