using System.Net.WebSockets;
using System.Text;

namespace cli.Utils;

public class WebsocketUtil
{
	private const int SEND_CHUNK_SIZE = 1024;
	public static async Task<string> ReadMessage(ClientWebSocket ws, CancellationToken cancelToken)
	{

		using var stream = new MemoryStream();
		WebSocketReceiveResult result = null;
		do
		{
			var read = new ArraySegment<byte>(new byte[4096]);
			result = await ws.ReceiveAsync(read, cancelToken);
			await stream.WriteAsync(read.Array, read.Offset, result.Count, cancelToken);
		} while (!result.EndOfMessage);

		stream.Seek(0, SeekOrigin.Begin);

		switch (result.MessageType)
		{
			case WebSocketMessageType.Text:
				var reader = new StreamReader(stream, Encoding.UTF8);
				var content = await reader.ReadToEndAsync();
				return content;
		}

		throw new NotImplementedException();
	}

	public static async Task SendMessageAsync(ClientWebSocket ws, string message, CancellationToken cancelToken)
	{
		if (ws.State != WebSocketState.Open)
		{
			throw new CliException($"Connection is not open. state=[{ws.State}]");
		}

		var messageBuffer = Encoding.UTF8.GetBytes(message);
		var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SEND_CHUNK_SIZE);
		
		for (var i = 0; i < messagesCount; i++)
		{
			var offset = (SEND_CHUNK_SIZE * i);
			var count = SEND_CHUNK_SIZE;
			var lastMessage = ((i + 1) == messagesCount);

			if ((count * (i + 1)) > messageBuffer.Length)
			{
				count = messageBuffer.Length - offset;
			}

			await ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text,
				lastMessage, cancelToken);
		}
	}

}
