using System.Collections.Immutable;
using System.Net;
using Amazon;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Amazon.Runtime;

namespace AutomationAWS;

public class LightsailAction : IDisposable
{
    private readonly AmazonLightsailClient _client;

    public LightsailAction(AWSCredentials credentials)
    {
        _client = new AmazonLightsailClient(credentials, RegionEndpoint.APSoutheast1);
    }

    public async Task Test()
    {
        var response = await _client.GetBlueprintsAsync(new());
        foreach (var blueprint in response.Blueprints.Select(b => b.BlueprintId))
        {
            Console.WriteLine(blueprint);
        }

        var response2 = await _client.GetBundlesAsync(new GetBundlesRequest());
        var bundles = response2.Bundles;
        bundles.Sort((b1, b2) => (int)(b1.Price - b2.Price));
        foreach (var bundle in bundles)
        {
            Console.WriteLine($"{bundle.Name} {bundle.BundleId} {bundle.Price}");
        }

        var regionsResponse =
            await _client.GetRegionsAsync(new GetRegionsRequest() { IncludeAvailabilityZones = true });
        foreach (var region in regionsResponse.Regions)
        {
            Console.WriteLine($"{region.Name} {region.DisplayName}");
        }
    }

    public async Task<string> CreateInstance(string password, int port)
    {
        var request = new CreateInstancesRequest()
        {
            AvailabilityZone = "ap-southeast-1a",
            BlueprintId = "debian_11",
            BundleId = "nano_3_0",
            InstanceNames = new() { "test" },
            UserData = $"""
                       export DEBIAN_FRONTEND=noninteractive
                       apt-get -qy clean
                       apt update -qy
                       apt upgrade -y -o "Dpkg::Options::=--force-confdef" -o "Dpkg::Options::=--force-confold"
                       apt install -qy tmux wget
                       wget https://github.com/shyn/dist/raw/master/snell-server
                       [ -e snell-server ] && chmod +x ./snell-server
                       cat >snell-server.conf <<EOF
                       [snell-server]
                       listen = 0.0.0.0:{port}
                       psk = {password}
                       ipv6 = false
                       obfs = http
                       EOF
                       tmux -c "./snell-server"
                       """
        };
        var response = await _client.CreateInstancesAsync(request);
        while (!checkRunning("test"))
        {
            Thread.Sleep(2000);
        }

        await _client.OpenInstancePublicPortsAsync(new OpenInstancePublicPortsRequest()
        {
            InstanceName = "test",
            PortInfo = new PortInfo()
            {
                FromPort = 8,
                ToPort = -1,
                Protocol = NetworkProtocol.Icmp
            }
        });
        await _client.OpenInstancePublicPortsAsync(new OpenInstancePublicPortsRequest()
        {
            InstanceName = "test",
            PortInfo = new PortInfo()
            {
                FromPort = port,
                ToPort = port,
                Protocol = NetworkProtocol.Tcp
            }
        });
        var info = await _client.GetInstanceAsync(new GetInstanceRequest()
        {
            InstanceName = "test"
        });
        Console.WriteLine(info.Instance.PublicIpAddress);
        #if DEBUG
        await Helpers.PingAsync(info.Instance.PublicIpAddress);
        #endif
        return info.Instance.PublicIpAddress;
    }

    private bool checkRunning(string name)
    {
        var ret = _client.GetInstanceStateAsync(new()
        {
            InstanceName = name
        }).Result;
        return ret.State.Name == "running";
    }

    public Task DestroyInstance(string name = "test")
    {
        try
        {
            _ = _client.GetInstanceAsync(new() { InstanceName = name }).Result;
        }
        catch (System.AggregateException aggregateException) when( aggregateException.InnerExceptions.First() is NotFoundException)
        {
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return _client.DeleteInstanceAsync(new DeleteInstanceRequest() { InstanceName = name });
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}