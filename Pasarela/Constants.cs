using Amazon.Lambda.Core;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Pasarela
{
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
}
