using System.Net;

namespace cli.Utils;

public class ObservableByteArrayContent : ByteArrayContent
{
	public float Progress { get; private set; }
	public int Position => _position;

	public Action<int, float> OnProgress;
	private int _position;
	private float _length;
	private byte[] _bytes;
	private readonly int _chunkSize;

	public ObservableByteArrayContent(byte[] content, int chunkSize=4096) : base(content)
	{
		_bytes = content;
		_chunkSize = chunkSize;
		_length = content.Length;
	}
	
	protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
	{
		var mem = new ReadOnlySpan<byte>(_bytes);
		
		
		for (int i = 0; i < _bytes.Length; i += _chunkSize)
		{
			var writeSize = Math.Min(_chunkSize, _bytes.Length - i);
			// await stream.WriteAsync(_bytes, i, writeSize);
			var chunk = mem.Slice(i, writeSize);
			stream.Write(chunk);
			var _ = stream.FlushAsync();
			_position = i + writeSize;
			Progress = _position / _length;
			OnProgress?.Invoke(i, Progress);
		}
	
		return Task.CompletedTask;
		// stream.Close();
	}

	// protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
	// {
	//
	// 	for (int i = 0; i < _bytes.Length; i += _chunkSize)
	// 	{
	// 		var writeSize = Math.Min(_chunkSize, _bytes.Length - i);
	// 		await stream.WriteAsync(_bytes, i, writeSize);
	// 		await stream.FlushAsync();
	// 		_position = i + writeSize;
	// 		Progress = _position / _length;
	// 		OnProgress?.Invoke(i, Progress);
	// 	}
	// 	
	// 	// stream.Close();
	// }
}
