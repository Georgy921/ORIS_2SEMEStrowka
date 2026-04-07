using System.Net.Sockets;
using Monopoly.Common.Protocol;

namespace Monopoly.Client.Services;

public class NetworkService : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly object _sendLock = new();
    private bool _isDisposed;

    public string? PlayerId { get; set; }
    public int ColorIndex { get; set; }
    public string? Nickname { get; set; }
    public bool IsConnected => _client?.Connected ?? false;

    public event EventHandler<GameMessage>? MessageReceived;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            _client.NoDelay = true;

            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _client.ConnectAsync(host, port, connectCts.Token);

            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

            return true;
        }
        catch
        {
            Disconnect();
            return false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _stream != null)
            {
                var message = await ReceiveMessageAsync(ct);
                if (message == null) break;

                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException) { }
        catch { }
        finally
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task<GameMessage?> ReceiveMessageAsync(CancellationToken ct)
    {
        if (_stream == null) return null;

        try
        {
            var lengthBuffer = new byte[4];
            int totalRead = 0;

            while (totalRead < 4)
            {
                int read = await _stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, ct);
                if (read == 0) return null;
                totalRead += read;
            }

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 1024 * 1024) return null;

            var dataBuffer = new byte[length];
            totalRead = 0;

            while (totalRead < length)
            {
                int read = await _stream.ReadAsync(dataBuffer, totalRead, length - totalRead, ct);
                if (read == 0) return null;
                totalRead += read;
            }

            return GameMessage.FromBytes(dataBuffer);
        }
        catch
        {
            return null;
        }
    }

    public void SendMessage(GameMessage message)
    {
        if (_stream == null || _isDisposed) return;

        lock (_sendLock)
        {
            try
            {
                message.SenderId = PlayerId ?? "";
                var data = message.ToBytes();
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch { }
        }
    }

    public void Disconnect()
    {
        if (_isDisposed) return;

        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Disconnect();
        _cts?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
    }
}