using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace LogWatcherServer.Controllers;

internal class WebSocketClient(ConcurrentBag<WebSocket> clients, Channel<string> messageChannel)
{
    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Broadcast Service started.");

        await foreach (var message in messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await BroadcastMessageAsync(message, clients);
        }
    }

    internal async Task BroadcastMessageAsync(string message, ConcurrentBag<WebSocket> clients)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(messageBytes);

        var tasks = new List<Task>();
        foreach (var client in clients)
        {
            if (client.State == WebSocketState.Open)
            {
                tasks.Add(client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None));
            }
        }

        await Task.WhenAll(tasks);
    }
}
