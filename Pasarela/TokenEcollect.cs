using Amazon.DynamoDBv2.DataModel;
using System;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{
    [DynamoDBTable("TokenEcollect")]
    public class TokenEcollect
    {
        [DynamoDBHashKey]
        public string Id { get; set; }
        [DynamoDBProperty]
        public string SessionToken { get; set; }
        [DynamoDBProperty]
        public DateTime FechaExpiracion { get; set; }
     

    }


}
