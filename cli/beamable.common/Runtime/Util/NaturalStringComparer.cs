using System.Collections.Generic;

namespace Beamable.Common.Util
{
	public class NaturalStringComparer: IComparer<string>
	{
		public int Compare(string val1, string val2)
		{
			if (val1 == null && val2 == null) return 0;
			if (val1 == null) return -1;
			if (val2 == null) return 1;
        
			int i = 0, j = 0;
        
			while (i < val1.Length && j < val2.Length)
			{
				if (char.IsDigit(val1[i]) && char.IsDigit(val2[j]))
				{
					int numX = 0, numY = 0;
                
					while (i < val1.Length && char.IsDigit(val1[i]))
					{
						numX = numX * 10 + (val1[i] - '0');
						i++;
					}
                
					while (j < val2.Length && char.IsDigit(val2[j]))
					{
						numY = numY * 10 + (val2[j] - '0');
						j++;
					}
                
					if (numX != numY)
						return numX.CompareTo(numY);
				}
				else
				{
					int compare = char.ToUpperInvariant(val1[i]).CompareTo(char.ToUpperInvariant(val2[j]));
					if (compare != 0)
						return compare;
                
					i++;
					j++;
				}
			}
        
			return val1.Length.CompareTo(val2.Length);
		}
	}
}
