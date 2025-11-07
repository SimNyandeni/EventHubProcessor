using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventHubProcessor;

public class Settings
{
    public string? EventHubConnectionString { get; set; }
    public string? EventHubName { get; set; }
    public string? SqlConnectionString { get; set; }
}

public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices(services =>
            {
                services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Values").Bind(settings);
                });
            })
            .Build();
        host.Run();
    }
}