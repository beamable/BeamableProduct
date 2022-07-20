using System.Collections;

namespace GeneratorExample;

public class Example
{
	public int Add()
	{
		return 3 + 2;
	}

	public int[] GetData()
	{
		return new int[] { 1, 2, 3, 4 };
	}

	public int[] Double(int[] data)
	{
		var doubles =  new int[data.Length];
		for (var i = 0; i < data.Length; i++)
		{
			doubles[i] = data[i] * 2;
		}

		return doubles;
	}

	public IEnumerable<int> Double2(int[] data)
	{
		for (var i = 0; i < data.Length; i++)
		{
			yield return data[i] * 2;
		}
	}
}
