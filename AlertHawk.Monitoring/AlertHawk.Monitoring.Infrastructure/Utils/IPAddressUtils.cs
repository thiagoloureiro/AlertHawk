using System.Diagnostics.CodeAnalysis;
using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace AlertHawk.Monitoring.Infrastructure.Utils;

[ExcludeFromCodeCoverage]
public static class IPAddressUtils
{
    public static async Task<MonitorRegion> GetLocation()
    {
        var ipAddress = await GetIPAddress();

        var apikey = Environment.GetEnvironmentVariable("ipgeo_apikey");

        var apiUrl = $"https://api.ipgeolocation.io/ipgeo?apiKey={apikey}&ip={ipAddress}";

        using var client = new HttpClient();

        MonitorRegion region = MonitorRegion.Europe;
        var json = await client.GetStringAsync(apiUrl);
        dynamic data = JObject.Parse(json);

        switch (data.continent_name.ToString())
        {
            case "Europe":
                region = MonitorRegion.Europe;
                break;
            case "Asia":
                region = MonitorRegion.Asia;
                break;
            case "North America":
                region = MonitorRegion.NorthAmerica;
                break;
            case "South America":
                region = MonitorRegion.SouthAmerica;
                break;
            case "Oceania":
                region = MonitorRegion.Oceania;
                break;
            case "Africa":
                region = MonitorRegion.Africa;
                break;
        }

        return region;
    }

    private static async Task<string> GetIPAddress()
    {
        try
        {
            using var ipAddress = new HttpClient();
            var ip = await ipAddress.GetStringAsync("http://icanhazip.com");
            return ip.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting IP address: {ex.Message}");
            throw;
        }
    }
}