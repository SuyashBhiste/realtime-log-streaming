using LogWatcherServer.Services;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

namespace LogWatcherServer.Controllers;

internal class WebSocketServer
{
    private readonly HttpListener _listener;
    private readonly ConcurrentBag<WebSocket> _clients;
    private readonly FileReaderService _fileReaderService;
    private readonly WebSocketClient _webSocketClient;

    public WebSocketServer(string urlPrefix, ConcurrentBag<WebSocket> clients, 
        FileReaderService fileReaderService, WebSocketClient webSocketClient)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(urlPrefix);
        _clients = clients;
        _fileReaderService = fileReaderService;
        _webSocketClient = webSocketClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.WriteLine("WebSocket Listener started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                await ProcessWebSocketRequestAsync(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        _listener.Stop();
    }

    private async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
    {
        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            _clients.Add(webSocket);
            Console.WriteLine("WebSocket connection established.");

            var lines = _fileReaderService.ReadLastLines(10);
            for (int i = 0; i < lines.Length; i++)
            {
                await _webSocketClient.BroadcastMessageAsync(lines[i], [webSocket]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing WebSocket request: {ex.Message} - {ex.StackTrace}");
        }
    }
}
