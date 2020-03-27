using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;
using Microsoft.Graph;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using static Pasarela.Function;
using static Pasarela.Constants;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Pasarela
{
    public class Function
    {
        public IConfigurationService ConfigService { get; }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<TokenEcollect> FunctionHandler(string input, ILambdaContext context)
        {

            var serviceCollection = new ServiceCollection();


            serviceCollection.AddLogging(b =>
            {
                b.AddFilter((category, level) => true); // Spam the world with logs.


            });
            ConfigureServices(serviceCollection);
            var services = serviceCollection.BuildServiceProvider();


            var ecollectToken = services.GetRequiredService<EcollectTokenClient>();


            var response = await ecollectToken.GetJson(ConfigService);

            var data = await response.Content.ReadAsAsync<TokenEcollect>();
            SaveToken(data);
            LambdaLogger.Log("Comprobar variable: ");
            //LambdaLogger.Log(ConfigService.GetConfiguration()[input] ?? "None");
            return data;
        }

        public static void SaveToken(TokenEcollect respuestaPasarela)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            //var context = new DynamoDBContext(client);
            Table tableApiKey = Table.LoadTable(client, "ApiKey");
            _ = PutDataAsync(tableApiKey, respuestaPasarela).Result;

        }

        private static async Task<string> PutDataAsync(Table table, TokenEcollect respuestaPasarela)
        {
            try
            {

                var doc = new Document
                {
                    ["Id"] = Guid.NewGuid(),
                    ["SessionToken"] = respuestaPasarela.SessionToken,
                    ["LifetimeSecs"] = DateTime.Now.AddSeconds(Convert.ToDouble(respuestaPasarela.LifetimeSecs))
                };

                Document x = await table.PutItemAsync(doc);

                return "success";
            }
            catch
            {
                throw;
            }
        }

        public Function()
        {
            // Set up Dependency Injection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Get Configuration Service from DI system
            ConfigService = serviceProvider.GetService<IConfigurationService>();
        }

        public interface IConfigurationService
        {
            IConfiguration GetConfiguration();
        }

        public interface IEnvironmentService
        {
            string EnvironmentName { get; set; }
        }

        private static void ConfigureServices(IServiceCollection services)
        {

            services.AddTransient<IEnvironmentService, EnvironmentService>();
            services.AddTransient<IConfigurationService, ConfigurationService>();

            var registry = services.AddPolicyRegistry();

            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            var longTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

            registry.Add("regular", timeout);
            registry.Add("long", longTimeout);

            services.AddHttpClient("ecollectToken", c =>
            {

            })

            // Build a totally custom policy using any criteria
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))

            // Use a specific named policy from the registry. Simplest way, policy is cached for the
            // lifetime of the handler.
            .AddPolicyHandlerFromRegistry("regular")

            // Run some code to select a policy based on the request
            .AddPolicyHandler((request) =>
            {
                return request.Method == HttpMethod.Get ? timeout : longTimeout;
            })

            // Run some code to select a policy from the registry based on the request
            .AddPolicyHandlerFromRegistry((reg, request) =>
            {
                return request.Method == HttpMethod.Get ?
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("regular") :
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("long");
            })

            // Build a policy that will handle exceptions, 408s, and 500s from the remote server
            .AddTransientHttpErrorPolicy(p => p.RetryAsync())

            .AddHttpMessageHandler(() => new RetryHandler()) // Retry requests to github using our retry handler
            .AddTypedClient<EcollectTokenClient>();
        }
    }


    public class EcollectTokenClient
    {
        public EcollectTokenClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; }

        // Gets the list of services on github.
        public async Task<HttpResponseMessage> GetJson(IConfigurationService ConfigService)
        {
            string mensaje = JsonConvert.SerializeObject(
                    new ContentTokenEcollect
                    {
                        EntityCode = ConfigService.GetConfiguration()["EntityCode"],
                        ApiKey = ConfigService.GetConfiguration()["ApiKey"]
                    });
            StringContent content = new StringContent(mensaje, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(ConfigService.GetConfiguration()["uri"], content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return response;
        }
    }

    public class ContentTokenEcollect
    {
        public string ApiKey { get; set; }
        public string EntityCode { get; set; }
    }

    public class TokenEcollect
    {
        public string SessionToken { get; set; }
        public string LifetimeSecs { get; set; }
        public string ReturnCode { get; set; }
    }

    public class EnvironmentService : IEnvironmentService
    {
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(EnvironmentVariables.AspnetCoreEnvironment)
                ?? Environments.Production;
        }

        public string EnvironmentName { get; set; }
    }

    public static class Constants
    {
        public static class EnvironmentVariables
        {
            public const string AspnetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        }

        public static class Environments
        {
            public const string Production = "Production";
        }
    }

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
