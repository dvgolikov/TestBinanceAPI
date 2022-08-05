// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TestBinanceAPI.Models;

while (true)
{
    Console.WriteLine("Test binance API.");
    Console.WriteLine("info btcusdt - get asset info");
    Console.WriteLine("subscribe btcusdt - get Aggregate Trade Streams (1000 packeges)");
    Console.WriteLine("Write new commnd:");
    string? command = Console.ReadLine();

    if (string.IsNullOrEmpty(command))
    {
        Console.WriteLine("Bad command. Try again.\n\n\n");
        continue;
    }
    if (command.Split(' ')[0] == "info")
    {
        await InfoCommand(command.Split(' ')[1]);

        Console.WriteLine("Command executed.\n\n\n");

        continue;
    }

    if (command.Split(' ')[0] == "subscribe")
    {
        await SubscribeCommand(command.Split(' ')[1]);

        Console.WriteLine("Command executed.\n\n\n");

        continue;
    }

    Console.WriteLine("Bad command. Try again.\n\n\n");
}


static async Task InfoCommand(string asset)
{
    HttpClient client = new HttpClient();

    try
    {
        HttpResponseMessage response = await client.GetAsync($"https://api.binance.com/api/v3/exchangeInfo?symbol={asset.ToUpper()}");
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        AssetInfo? assetInfo = JsonSerializer.Deserialize<AssetInfo>(responseBody);

        if (assetInfo == null) Console.WriteLine("\nNo data received.");

        var symbol = assetInfo?.symbols?[0];

        Console.WriteLine($"Symbol:{symbol?.symbol}; BaseAsset:{symbol?.baseAsset}; Status:{symbol?.status}; QuoteAsset:{symbol?.quoteAsset}; ");
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine("\nException Caught!");
        Console.WriteLine("Message :{0} ", e.Message);
    }
}

static async Task SubscribeCommand(string asset)
{
    string SocketUrl = @"wss://stream.binance.com:9443/ws";

    using var socket = new ClientWebSocket();

    try
    {
        await socket.ConnectAsync(new Uri(SocketUrl), CancellationToken.None);


        var request = new BinanceRequest() { id = 1, method = "SUBSCRIBE", @params = new string[] { $"{asset.ToLower()}@aggTrade" } };
        var requestString = JsonSerializer.Serialize(request);

        await Send(socket, requestString);

        Stopwatch stopwatch = Stopwatch.StartNew();

        await ReceiveData(socket);

        request = new BinanceRequest() { id = 2, method = "UNSUBSCRIBE", @params = new string[] { $"{asset.ToLower()}@aggTrade" } };
        requestString = JsonSerializer.Serialize(request);

        await Send(socket, requestString);

        stopwatch.Stop();

        Console.WriteLine($"Receive time: {stopwatch.ElapsedMilliseconds}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR - {ex.Message}");
    }
}

static async Task Send(ClientWebSocket socket, string data) =>
    await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);

static async Task ReceiveData(ClientWebSocket socket)
{
    int packetCounter = 0;

    var buffer = new ArraySegment<byte>(new byte[2048]);
    do
    {
        WebSocketReceiveResult result;
        using var ms = new MemoryStream();
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            ms.Write(buffer.Array, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Close)
            break;

        ms.Seek(0, SeekOrigin.Begin);

        using (var reader = new StreamReader(ms, Encoding.UTF8))
        {
            var binanceResponce = JsonSerializer.Deserialize<BinanceResponce>(await reader.ReadToEndAsync());
            Console.WriteLine(binanceResponce?.ToString());
        }

        packetCounter++;
    } while (packetCounter <= 1000);
}