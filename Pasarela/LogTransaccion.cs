using Amazon.DynamoDBv2.DataModel;
using System;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{
    [DynamoDBTable("LogTransaccion")]
    public class LogTransaccion
    {
        [DynamoDBHashKey]
        public string Id { get; set; }
      
        [DynamoDBProperty]
        public string NombreServicio { get; set; }
        [DynamoDBProperty]
        public DateTime FechaTransaccion { get; set; }
        [DynamoDBProperty]
        public string Request { get; set; }
        [DynamoDBProperty]
        public string Response { get; set; }

    }


}
