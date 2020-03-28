using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{

    public class TokenEcollectResponse
    {
      
     
        public string SessionToken { get; set; }
       
        public string LifetimeSecs { get; set; }
        
        public string ReturnCode { get; set; }

     
    }


}
