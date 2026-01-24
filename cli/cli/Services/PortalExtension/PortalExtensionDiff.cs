using spkl.Diffs;

namespace cli.Services.PortalExtension;

[Serializable]
public class DiffSingleInstruction
{
	public int LineA;
	public int LineB;
	public int CountA;
	public int CountB;
}

[Serializable]
public class DiffInstructions
{
	public bool HasChanges;
	public string[] LinesToAdd;
	public DiffSingleInstruction[] Instructions;
}

public class PortalExtensionDiff
{
	public static DiffInstructions GetDiffInstructions(string[] fileLinesA, string[] fileLinesB)
	{
		MyersDiff<string> diff = new MyersDiff<string>(fileLinesA, fileLinesB);

		List<string> linesToAdd = new List<string>();
		List<DiffSingleInstruction> instructions = new List<DiffSingleInstruction>();

		var editScript = diff.GetEditScript().ToArray();

		if (editScript.Length == 0)
		{
			return new DiffInstructions() { HasChanges = false };
		}

		foreach ((int lA, int lB, int cA, int cB) in editScript)
		{
			instructions.Add(new DiffSingleInstruction()
			{
				CountA = cA,
				CountB = cB,
				LineA = lA,
				LineB = lB
			});

			if (cB > 0)
			{
				for (int i = 0; i < cB; i++)
				{
					linesToAdd.Add(fileLinesB[lB + i]);
				}
			}
		}

		return new DiffInstructions() { Instructions = instructions.ToArray(), LinesToAdd = linesToAdd.ToArray(), HasChanges = true};
	}

	public static string[] GetResult(MyersDiff<string> diff, string[] a, string[] linesToAdd)
	{
		int currentAIndex = 0;
		int currentAddedIndex = 0;

		List<string> result = new List<string>();

		foreach ((int LineA, int LineB, int CountA, int CountB) in diff.GetEditScript())
		{
			int unchangedCount = LineA - currentAIndex;

			if (unchangedCount > 0)
			{
				for (int i = 0; i < unchangedCount; i++)
				{
					result.Add(a[currentAIndex + i]);
				}
			}

			currentAIndex = LineA;

			currentAIndex += CountA;

			if (CountB > 0)
			{
				for (int i = 0; i < CountB; i++)
				{
					result.Add(linesToAdd[currentAddedIndex]);
					currentAddedIndex++;
				}
			}
		}

		if (currentAIndex < a.Length)
		{
			for (int i = currentAIndex; i < a.Length; i++)
			{
				result.Add(a[i]);
			}
		}

		return result.ToArray();
	}
}
