using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using static Pasarela.Function;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{
    public class ConfigurationService : IConfigurationService
    {
        public IEnvironmentService EnvService { get; }

        public ConfigurationService(IEnvironmentService envService)
        {
            EnvService = envService;
        }

        public IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{EnvService.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
