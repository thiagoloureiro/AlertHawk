using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace AlertHawk.Monitoring.Infrastructure.Utils;

public static class IPAddressUtils
{
    public static LocationDetails GetLocation()
    {
        string ipAddress = GetIPAddress();

        string apikey = Environment.GetEnvironmentVariable("ipgeo_apikey");

        string apiUrl = $"https://api.ipgeolocation.io/ipgeo?apiKey={apikey}&ip={ipAddress}";

        using WebClient client = new WebClient();
        try
        {
            string json = client.DownloadString(apiUrl);
            dynamic data = JObject.Parse(json);

            var locationDetails = new LocationDetails
            {
                Country = data.country_name,
                Continent = data.continent_name
            };

            return locationDetails;
        }
        catch (WebException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    private static string GetIPAddress()
    {
        try
        {
            using var ipAddress = new WebClient();
            var ip = ipAddress.DownloadString("http://icanhazip.com");
            return ip.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting IP address: {ex.Message}");
            throw;
        }
    }
}