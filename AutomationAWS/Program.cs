// See https://aka.ms/new-console-template for more information

using Amazon.Runtime;
using AutomationAWS;

var password = Environment.GetEnvironmentVariable("psk")!;
var port = int.Parse(Environment.GetEnvironmentVariable("port")!);
var awsKey = Environment.GetEnvironmentVariable("aws_key")!;
var awsSecret = Environment.GetEnvironmentVariable("aws_secret")!;

var cfZone = Environment.GetEnvironmentVariable("cf_zone")!;
var cfApiToken = Environment.GetEnvironmentVariable("cf_apitoken")!;

var command = args.FirstOrDefault("create");
var lightsailAction = new LightsailAction(new BasicAWSCredentials(awsKey, awsSecret));

if (command == "destroy")
{
    await lightsailAction.DestroyInstance();
}
else if (command == "create")
{
    await lightsailAction.DestroyInstance();
    var ip = await lightsailAction.CreateInstance(password, port);

    var cloudflareApi = new CloudflareApi(cfZone, cfApiToken);
    await cloudflareApi.CreateOrUpdateAsync("test", ip);
}