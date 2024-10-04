namespace LogWatcherServer.Services;

internal class FileReaderService(string filePath, int initialBufferSize = 1024)
{
    internal string[] ReadLastLines(int numberOfLines)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);

        if (numberOfLines <= 0)
        {
            throw new ArgumentException("Number of lines to read must be greater than zero.");
        }

        var lines = new LinkedList<string>();

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    long position = stream.Length;
                    int bufferSize = initialBufferSize;

                    while (position > 0 && lines.Count < numberOfLines)
                    {
                        int bytesToRead = GetBufferSize(ref position, bufferSize);
                        var buffer = ReadBuffer(stream, reader, position, bytesToRead);
                        ProcessBuffer(buffer, ref lines, ref bufferSize, numberOfLines);
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.");
        }
        catch (IOException ex)
        {
            throw new IOException($"An I/O error occurred while reading the file: {ex.Message}", ex);
        }

        return [..lines];
    }

    private int GetBufferSize(ref long position, int bufferSize)
    {
        int bytesToRead = position >= bufferSize ? bufferSize : (int)position;
        position -= bytesToRead;
        return bytesToRead;
    }

    private char[] ReadBuffer(FileStream stream, StreamReader reader, long position, int bytesToRead)
    {
        stream.Seek(position, SeekOrigin.Begin);
        reader.DiscardBufferedData(); 

        var buffer = new char[bytesToRead];
        reader.ReadBlock(buffer, 0, bytesToRead);

        return buffer;
    }

    private void ProcessBuffer(char[] buffer, ref LinkedList<string> lines, ref int bufferSize, int numberOfLines)
    {
        var currentLine = new LinkedList<char>();

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (buffer[i] == '\n' && currentLine.Count > 0)
            {
                lines.AddFirst(new string(currentLine.ToArray()));
                currentLine.Clear();

                if (lines.Count == numberOfLines)
                    return;
            }

            if (buffer[i] != '\r')  
                currentLine.AddFirst(buffer[i]);
        }

        lines.AddFirst(new string(currentLine.ToArray()));
        currentLine.Clear();

        bufferSize = currentLine.Count > 0 ? bufferSize *= 2 : initialBufferSize;
    }
}