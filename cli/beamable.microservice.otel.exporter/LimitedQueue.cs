using System.Collections;

namespace beamable.microservice.otel.exporter;

public class LimitedQueue<T> : IEnumerable<T>
{
	private readonly Queue<T> _queue = new();
	private readonly int _maxSize;

	public int Count => _queue.Count;

	public LimitedQueue(int maxSize)
	{
		_maxSize = maxSize;
	}

	public void Enqueue(T item)
	{
		if (_queue.Count >= _maxSize)
		{
			_queue.Dequeue();
		}
		_queue.Enqueue(item);
	}

	public T Dequeue()
	{
		if (_queue.Count == 0)
		{
			throw new InvalidOperationException(
				$"Limited Queue of type=[{typeof(T)}] doesn't have any values to dequeue");
		}

		return _queue.Dequeue();
	}

	public void Clear()
	{
		_queue.Clear();
	}

	public bool Contains(T item)
	{
		return _queue.Contains(item);
	}

	public T[] ToArray()
	{
		return _queue.ToArray();
	}

	public List<T> ToList()
	{
		return _queue.ToList();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _queue.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
