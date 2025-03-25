using UnityEngine;

namespace Beamable.Server.Common;

public class BigStream : Stream
{
    private static readonly int MAX_STREAM_SIZE = (int)Math.Pow(2, 30);
    
    public int currentStreamIndex;
    public List<MemoryStream> innerStreams = new List<MemoryStream>();
    
    public int MaxStreamSize { get; } = MAX_STREAM_SIZE;

    public BigStream(int maxStreamSize=-1)
    {
        if (maxStreamSize <= 0)
        {
            maxStreamSize = MAX_STREAM_SIZE;
        }

        MaxStreamSize = maxStreamSize;
        
        currentStreamIndex = 0;
        innerStreams.Add(CreateInnerStream());
    }

    private MemoryStream CreateInnerStream() => new MemoryStream();
    
    public override void Flush()
    {
        innerStreams[currentStreamIndex].Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var toRead = count;
        var read = 0;
        while (toRead > 0)
        {
            var currentStream = innerStreams[currentStreamIndex];
            var availableToRead = MaxStreamSize - (int)currentStream.Position;
            if (availableToRead <= 0)
            {
                currentStreamIndex++;
                if (currentStreamIndex >= innerStreams.Count)
                {
                    // there is no data left!
                    return read;
                }
                continue;
            }
            var readableCount = Math.Min(availableToRead, toRead);
            var thisRead = currentStream.Read(buffer, offset + read, readableCount);
            if (thisRead == 0)
            {
                return read;
            }
            
            read += thisRead;
            toRead -= read;
        }

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var originPosition = 0L;
        switch (origin)
        {
            case SeekOrigin.Begin:
                originPosition = 0;
                break;
            case SeekOrigin.Current:
                originPosition = Position;
                break;
            case SeekOrigin.End:
                originPosition = Length;
                break;
        }

        var p = Position = originPosition + offset;
        return p;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("cannot set the length of a " + nameof(BigStream));
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var toWrite = count;
        var written = 0;
        while (toWrite > 0)
        {
            var currentStream = innerStreams[currentStreamIndex];
            var availableSpace = MaxStreamSize - (int)currentStream.Position;
            if (availableSpace <= 0)
            {
                currentStreamIndex++;
                innerStreams.Add(CreateInnerStream());
                continue;
            }

            var writableCount = Math.Min(availableSpace, toWrite);
            currentStream.Write(buffer, offset + written, writableCount);
            written += writableCount;
            toWrite -= writableCount;
        }

    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;

    public override long Length
    {
        get
        {
            var size = 0L;
            foreach (var stream in innerStreams)
            {
                size += stream.Length;
            }

            return size;
        }
    }

    public override long Position
    {
        get => Math.Max(0, currentStreamIndex)*MaxStreamSize + innerStreams[currentStreamIndex].Position;
        set
        {
            var pos = value;

            var mid = pos % MaxStreamSize;
            var index = pos / MaxStreamSize;
            currentStreamIndex = (int)index;
            for (var i = 0; i < innerStreams.Count; i++)
            {
                if (currentStreamIndex > i)
                {
                    innerStreams[i].Position = MaxStreamSize - 1;
                } else if (currentStreamIndex == i)
                {
                    innerStreams[i].Position = mid;
                }
                else
                {
                    innerStreams[i].Position = 0;
                }
            }
        }
    }
}