using System.Threading.Channels;

namespace LogWatcherServer.Services;

internal class FileWatcherService
{
    private readonly string _filePath;
    private long _lastReadPosition = 0;  
    private readonly object _fileLock = new();  
    private readonly Channel<string> _lineChannel;
    private readonly FileSystemWatcher _fileWatcher;
    private volatile bool _isFileModified = false;

    public FileWatcherService(string filePath, Channel<string> lineChannel)
    {
        _filePath = filePath;
        _lineChannel = lineChannel;

        _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("File change noticed");
        if (e.FullPath == _filePath)
        {
            _isFileModified = true;
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        _lastReadPosition = GetFileLength();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_isFileModified)
                {
                    _isFileModified = false;
                    await ProcessNewLinesAsync();
                }

                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    private async Task ProcessNewLinesAsync()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine("File no longer exists.");
                return;
            }

            using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    fileStream.Seek(_lastReadPosition, SeekOrigin.Begin);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _lineChannel.Writer.TryWrite(line);
                    }

                    _lastReadPosition = fileStream.Position;
                }
            }
        }
    }

    private long GetFileLength()
    {
        try
        {
            var fileInfo = new FileInfo(_filePath);
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting file length: {ex.Message}");
            return 0;
        }
    }

    public void StopMonitoring()
    {
        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Dispose();
    }
}