using System.Net.Http.Headers;
using CloudFlare.Client;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;

namespace AutomationAWS;

public class CloudflareApi
{
    private HttpClient _client;

    private CloudFlareClient cloudFlareClient;
    public string ZoneIdentifier { get; set; }

    public CloudflareApi(string zoneIdentifier, string apiToken)
    {
        // _client = new HttpClient()
        // {
        //     BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        // };
        // _client.DefaultRequestHeaders.Accept.Add(new("application/json"));
        ZoneIdentifier = zoneIdentifier;
        cloudFlareClient = new CloudFlareClient(apiToken);
    }

    public async Task Search(string name)
    {
        var response = await _client.GetAsync($"zones/{ZoneIdentifier}/dns_records");
        Console.WriteLine(response?.RequestMessage?.RequestUri);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine(result);
    }

    public async Task CreateOrUpdateAsync(string name, string ip, bool proxy = false)
    {
        var searchResult =
            await cloudFlareClient.Zones.DnsRecords.GetAsync(ZoneIdentifier,
                new DnsRecordFilter() { Name = name + ".lovex.in" });
        if (searchResult.Result.Count == 0)
        {
            var ret = await cloudFlareClient.Zones.DnsRecords.AddAsync(ZoneIdentifier, new NewDnsRecord()
            {
                Type = DnsRecordType.A,
                Name = name,
                Content = ip,
                Proxied = proxy
            });
        }
        else
        {
            var id = searchResult.Result.First().Id;
            Console.WriteLine(id);
            var updateResponse = await cloudFlareClient.Zones.DnsRecords.UpdateAsync(ZoneIdentifier, id,
                new ModifiedDnsRecord()
                {
                    Content = ip,
                    Name = name,
                    Type = DnsRecordType.A
                });
            if (!updateResponse.Success)
            {
                // exception handling
            }
        }
    }
}