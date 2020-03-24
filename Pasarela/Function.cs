using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

using Amazon.Lambda.Core;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.DataModel;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Pasarela
{
    public static class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ResponseEcollect FunctionHandler(ILambdaContext context)
        {
            
            return ProcessRepositories();
        }
        private static ResponseEcollect ProcessRepositories()
        {
            ResponseEcollect respuestaEcollect;

            try
            {
                string mensaje = JsonConvert.SerializeObject(
                   new ContentEcollect
                   {
                       EntityCode = "41610",
                       ApiKey = "4437637A572B766F4F587934664A62334E383577304D79657152584E77557331"
                   });
                StringContent content = new StringContent(mensaje, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {


                    Task<HttpResponseMessage> res = client.PostAsync("https://test3.e-collect.com/app_express/api/getSessionToken"
                      , content

                    );

                    LambdaLogger.Log(" va a consumir servicio1");


                    string respuesta = res.Result.Content.ReadAsStringAsync().Result;

                    respuestaEcollect = JsonConvert.DeserializeObject<ResponseEcollect>(respuesta);

                    Insertar(respuestaEcollect);
                    return respuestaEcollect;

                }

            }
            catch 
            {
                throw;
               
            }



        }

        public static void Insertar(ResponseEcollect respuestaPasarela)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            //var context = new DynamoDBContext(client);
            Table tableApiKey = Table.LoadTable(client, "ApiKey");
            _ = PutDataAsync(tableApiKey, respuestaPasarela).Result;

        }


        private static async Task<string> PutDataAsync(Table table, ResponseEcollect respuestaPasarela)
        {
            try
            {

                var doc = new Document
                {
                    ["Id"] = Guid.NewGuid(),
                    ["SessionToken"] = respuestaPasarela.SessionToken,  
                    ["LifetimeSecs"] = respuestaPasarela.LifetimeSecs 
                };

                Document x = await table.PutItemAsync(doc);
         
                return "success";
            }
            catch 
            {
                throw;
            }
        }
       


    }




    public class ContentEcollect
    {
        public string ApiKey { get; set; }
        public string EntityCode { get; set; }
    }
    public class ResponseEcollect
    {
        public string SessionToken;
        public string LifetimeSecs;
        public string ReturnCode;
    }

}

