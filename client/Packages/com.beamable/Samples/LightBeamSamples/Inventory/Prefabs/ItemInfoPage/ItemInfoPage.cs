using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPage : MonoBehaviour, ILightComponent<PlayerItem>
{
	[Header("Scene references")]
	public Image icon;
	public TextMeshProUGUI id;
	public TextMeshProUGUI type;
	public TextMeshProUGUI createdDate;
	public TextMeshProUGUI updatedDate;
	public Transform propertiesContainer;
	public Button backButton;

	private LightBeam _beam;
	private PlayerItem _model;
	
	public Promise OnInstantiated(LightBeam beam, PlayerItem model)
	{
		_beam = beam;
		_model = model;
		model.OnUpdated += () =>
		{
			var _ = Refresh();
		};
		var _ = Refresh();
		
		backButton.HandleClicked(() =>
		{
			beam.GotoPage<HomePage>();
		});
		
		return Promise.Success;
	}
	
	private async Promise Refresh()
	{
		propertiesContainer.Clear();
		
		if (_model.Content.icon != null && _model.Content.icon.Asset != null)
		{
			icon.sprite = await _model.Content.icon.LoadSprite();
		}
		
		id.text = _model.ContentId;
		type.text = _model.Content.name;
		
		createdDate.text = $"Created: {GetDateTimeFromInt(_model.CreatedAt)}";
		updatedDate.text = $"Updated: {GetDateTimeFromInt(_model.UpdatedAt)}";

		var promises = new List<Promise<PropertyDisplayBehaviour>>();
		foreach (KeyValuePair<string,string> property in _model.Properties)
		{
			var data = new PropertyDisplayData() {Key = property.Key, Value = property.Value};
			var p = _beam.Instantiate<PropertyDisplayBehaviour, PropertyDisplayData>(propertiesContainer, data);
			promises.Add(p);
		}
		
		var sequence = Promise.Sequence(promises);
		await sequence;
	}
	
	private static string GetDateTimeFromInt(long dateAsLong)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(dateAsLong).ToShortDateString();
	}
}
