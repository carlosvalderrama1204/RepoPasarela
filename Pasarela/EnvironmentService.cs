using Amazon.Lambda.Core;
using System;
using static Pasarela.Constants;
using static Pasarela.Function;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.


namespace Pasarela
{
    public class EnvironmentService : IEnvironmentService
    {
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(EnvironmentVariables.AspnetCoreEnvironment)
                ?? Environments.Production;
        }

        public string EnvironmentName { get; set; }
    }
}
