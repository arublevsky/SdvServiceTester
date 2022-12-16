namespace ApiTester.Recommendations;

public class ImportSettings
{
    public string AuthToken { get; set; }

    public string ApiBase { get; set; }

    public long TotalRecords { get; set; }

    public int BatchSize { get; set; }

    public int RequestsParallelism { get; set; }

    public long TotalRequests => TotalRecords / BatchSize;
}