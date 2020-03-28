using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Polly;
using System;
using System.Net.Http;
using System.Threading.Tasks;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


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
        public async Task<TokenEcollectResponse> FunctionHandler(string input, ILambdaContext context)
        {




            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var services = serviceCollection.BuildServiceProvider();
            var ecollectToken = services.GetRequiredService<EcollectTokenClient>();
            var response = await ecollectToken.GetJson(ConfigService);

            var data = await response.Content.ReadAsAsync<TokenEcollectResponse>();
            SaveToken(data);
           
           
            return data;
        }

        public static void SaveToken(TokenEcollectResponse respuestaPasarela)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            
            Table tableApiKey = Table.LoadTable(client, "ApiKey");
            _ = PutDataAsync(tableApiKey, respuestaPasarela).Result;

        }

        private static async Task<string> PutDataAsync(Table table, TokenEcollectResponse respuestaPasarela)
        {
            try
            {

                TokenEcollect tokenEcollect = new TokenEcollect
                {
                    Id = Guid.NewGuid().ToString(),
                    SessionToken = respuestaPasarela.SessionToken,
                    FechaExpiracion = DateTime.Now.AddSeconds(Convert.ToDouble(respuestaPasarela.LifetimeSecs))

                };

                await new DynamoDBContext(new AmazonDynamoDBClient()).SaveAsync(tokenEcollect);
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
            services.AddLogging(b =>
            {
                b.AddFilter((category, level) => true); // Spam the world with logs.
            });

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
}
