using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private const string ApiKey = "your_api_key_here";
    private const string BaseUrl = "https://api.milaex.com/api/v1/exchange";
    private const double ArbitrageThreshold = 0.5;

    private static readonly List<(string key, string baseCurrency, string quoteCurrency)> Exchanges = new()
    {
        ("binance", "BTC", "USDT"),
        ("bitfinex", "BTC", "USD"),
        ("valr", "BTC", "USDT"),
        ("bitstamp", "BTC", "USDT"),
        ("coinbase", "BTC", "USDT"),
        ("coinex", "BTC", "USDT"),
        ("cryptocom", "BTC", "USDT"),
        ("gateio", "BTC", "USDT"),
        ("luno", "BTC", "USDC"),
        ("poloniex", "BTC", "USDT")
    };

    static async Task<double?> GetPriceAsync(HttpClient client, string exchange, string baseCurrency, string quoteCurrency)
    {
        try
        {
            var url = $"{BaseUrl}/ticker?exchange={exchange}&base_name={baseCurrency}&quote_name={quoteCurrency}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", ApiKey);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

            if (json.TryGetProperty("Data", out var data) && data.TryGetProperty("lastPrice", out var price))
                return price.GetDouble();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error from {exchange}: {ex.Message}");
            return null;
        }
    }

    static List<string> CheckArbitrage(Dictionary<string, double?> prices)
    {
        var opps = new List<string>();
        foreach (var buy in prices)
        {
            foreach (var sell in prices)
            {
                if (buy.Key == sell.Key || !buy.Value.HasValue || !sell.Value.HasValue)
                    continue;

                double diff = (sell.Value.Value - buy.Value.Value) / buy.Value.Value * 100;
                if (diff >= ArbitrageThreshold)
                {
                    opps.Add($"Buy from {buy.Key} at {buy.Value} → Sell to {sell.Key} at {sell.Value} → Profit: {Math.Round(diff, 2)}%");
                }
            }
        }
        return opps;
    }

    static async Task Main()
    {
        using var client = new HttpClient();

        while (true)
        {
            Console.WriteLine("Checking BTC prices...");
            var prices = new Dictionary<string, double?>();

            foreach (var (key, baseCurrency, quoteCurrency) in Exchanges)
            {
                var price = await GetPriceAsync(client, key, baseCurrency, quoteCurrency);
                prices[key] = price;
                Console.WriteLine($"{key}: {price} {quoteCurrency}");
            }

            Console.WriteLine("\nArbitrage Opportunities:");
            var opps = CheckArbitrage(prices);
            if (opps.Count > 0) opps.ForEach(Console.WriteLine);
            else Console.WriteLine("No arbitrage opportunities found.");

            Console.WriteLine("\nWaiting 15 seconds...\n");
            Thread.Sleep(15000);
        }
    }
}
