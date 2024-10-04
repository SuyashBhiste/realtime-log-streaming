using LogWatcherServer.Controllers;
using LogWatcherServer.Services;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;

string filePath = "G:\\Interview Browserstack\\Resources\\log.txt"; 
string urlPrefix = "http://localhost:8080/log/";

var lineChannel = Channel.CreateUnbounded<string>();

var fileReaderService = new FileReaderService(filePath);
var fileWatcherService = new FileWatcherService(filePath, lineChannel);

var clients = new ConcurrentBag<WebSocket>();
var webSocketClient = new WebSocketClient(clients, lineChannel);
var webSocketServer = new WebSocketServer(urlPrefix, clients, fileReaderService, webSocketClient);

var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

var fileWatcherTask = Task.Run(() => fileWatcherService.StartMonitoringAsync(cancellationToken), cancellationToken);

var webSocketClientTask = Task.Run(() => webSocketClient.StartAsync(cancellationToken), cancellationToken);

var webSocketServerTask = Task.Run(() => webSocketServer.StartAsync(cancellationToken), cancellationToken);

await Task.WhenAll(fileWatcherTask, webSocketClientTask, webSocketServerTask);

fileWatcherService.StopMonitoring();