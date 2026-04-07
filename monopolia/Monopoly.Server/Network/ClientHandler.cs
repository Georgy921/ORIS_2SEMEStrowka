using System.Net.Sockets;
using Monopoly.Common.Protocol;

namespace Monopoly.Server.Network;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly object _sendLock = new();
    private bool _isConnected = true;

    public ClientHandler(TcpClient client)
    {
        _client = client;
        _client.ReceiveTimeout = 0;
        _client.SendTimeout = 10000;
        _stream = client.GetStream();
    }

    public async Task<GameMessage?> ReceiveMessageAsync()
    {
        try
        {
            var lengthBuffer = new byte[4];
            int totalRead = 0;

            while (totalRead < 4)
            {
                int read = await _stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead);
                if (read == 0) return null;
                totalRead += read;
            }

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 1024 * 1024) return null;

            var dataBuffer = new byte[length];
            totalRead = 0;

            while (totalRead < length)
            {
                int read = await _stream.ReadAsync(dataBuffer, totalRead, length - totalRead);
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
        if (!_isConnected) return;

        lock (_sendLock)
        {
            try
            {
                var data = message.ToBytes();
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch
            {
                _isConnected = false;
            }
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        try
        {
            _stream.Close();
            _client.Close();
        }
        catch { }
    }
}