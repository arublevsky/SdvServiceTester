using Microsoft.Extensions.Configuration;

namespace ApiTester.Recommendations;

public class ImportConfigurationProvider
{
    public IConfigurationRoot Configuration { get; set; }

    public ImportConfigurationProvider()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        Configuration = builder.Build();
    }

    public ImportSettings ImportSettings => Configuration.GetSection("importSettings").Get<ImportSettings>();
}