using Beamable.Player.CloudSaving;
using Beamable.Runtime.LightBeams;
using System;
using System.Globalization;
using TMPro;
using UnityEngine.UI;

[Serializable]
public class ConflictViewReferences
{
	public TextMeshProUGUI Size;
	public TextMeshProUGUI LastModified;
	public TextMeshProUGUI CheckSum;
	public Button _chooseThis;

	public void SetValues(CloudSaveEntry entry, Action chooseThis)
	{
		Size.text = $"{entry.size} bytes";
		var dateTime =
			DateTime.ParseExact(entry.lastModified.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		LastModified.text = $"{dateTime.ToString(CultureInfo.InvariantCulture)}";
		CheckSum.text = entry.eTag;
		_chooseThis.HandleClicked(chooseThis);
	}
	
}
