using ApiTester.Recommendations;

using (var cts = new CancellationTokenSource())
{
    Console.CancelKeyPress += OnCancelKeyPress(cts);
    await Importer.Run(new ImportConfigurationProvider().ImportSettings, cts.Token);
}

ConsoleCancelEventHandler OnCancelKeyPress(CancellationTokenSource cts) => (_, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};