using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Pasarela.Function;




// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{
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

            string id = Guid.NewGuid().ToString();

            LogTransaccion logTransaccion = new LogTransaccion
            {
                Id = id,
                NombreServicio = "TokenEcollect",
                FechaTransaccion = DateTime.Now,
                Request = mensaje

            };
            DynamoDBContext context = new DynamoDBContext(new AmazonDynamoDBClient());
             await context.SaveAsync(logTransaccion);
            LogTransaccion logTransaccionRetrieved = await context.LoadAsync<LogTransaccion>(id);
           
            StringContent content = new StringContent(mensaje, Encoding.UTF8, "application/json");


            var response = await HttpClient.PostAsync(ConfigService.GetConfiguration()["uri"], content).ConfigureAwait(false);
            
           response.EnsureSuccessStatusCode();
            logTransaccionRetrieved.Response = await response.Content.ReadAsStringAsync();
            await context.SaveAsync(logTransaccionRetrieved);




            return response;
        }
    }
}
