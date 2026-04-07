using System.Text;
using System.Text.Json;

namespace Monopoly.Common.Protocol;

[Serializable]
public class GameMessage
{
    public MessageType Type { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Payload { get; set; } = string.Empty;

    public GameMessage()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public GameMessage(MessageType type) : this()
    {
        Type = type;
    }

    public GameMessage(MessageType type, object data) : this()
    {
        Type = type;
        Payload = JsonSerializer.Serialize(data);
    }

    public T? GetData<T>()
    {
        if (string.IsNullOrEmpty(Payload))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(Payload);
        }
        catch
        {
            return default;
        }
    }

    public byte[] ToBytes()
    {
        var json = JsonSerializer.Serialize(this);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(jsonBytes.Length);

        var result = new byte[4 + jsonBytes.Length];
        Array.Copy(lengthBytes, 0, result, 0, 4);
        Array.Copy(jsonBytes, 0, result, 4, jsonBytes.Length);

        return result;
    }

    public static GameMessage? FromBytes(byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<GameMessage>(json);
        }
        catch
        {
            return null;
        }
    }
}