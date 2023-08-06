using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ServerlessBlog.Frontend
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                })
                .Build();
            await host.RunAsync();
        }
    }
}