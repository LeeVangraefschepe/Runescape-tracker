namespace Runescape_tracker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            RuneMetricsClient metricsClient = new RuneMetricsClient();
            var result = await metricsClient.GetProfileAsync("lee_vgs");

            Console.WriteLine();
            Console.ReadLine();
        }
    }
}