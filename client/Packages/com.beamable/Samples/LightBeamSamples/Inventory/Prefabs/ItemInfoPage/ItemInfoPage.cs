using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using System.Globalization;
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
		model.OnUpdated += Refresh;
		Refresh();
		
		backButton.HandleClicked(() =>
		{
			beam.GotoPage<HomePage>();
		});
		
		return Promise.Success;
	}
	
	private void Refresh()
	{
		propertiesContainer.Clear();
		
		if (_model.Content.icon.Asset != null)
		{
			_model.Content.icon.LoadSprite().Then((sprite) =>
			{
				icon.sprite = sprite;
			});
		}
		
		id.text = _model.ContentId;
		type.text = _model.Content.name;
		
		
		
		
		createdDate.text = $"Created: {GetDateTimeFromInt(_model.CreatedAt)?.ToShortDateString()}";
		updatedDate.text = $"Updated: {GetDateTimeFromInt(_model.UpdatedAt)?.ToShortDateString()}";

		foreach (KeyValuePair<string,string> property in _model.Properties)
		{
			var data = new PropertyDisplayData() {Key = property.Key, Value = property.Value};
			_beam.Instantiate<PropertyDisplayBehaviour, PropertyDisplayData>(propertiesContainer, data);
		}
	}
	
	private static DateTime? GetDateTimeFromInt(long? dateAsLong, bool hasTime = true)
	{
		if (dateAsLong.HasValue && dateAsLong > 0)
		{
			if (hasTime)
			{
				// sometimes input is 14 digit and sometimes 16
				var numberOfDigits = (int)Math.Floor(Math.Log10(dateAsLong.Value) + 1);

				if (numberOfDigits > 14)
				{
					dateAsLong /= (int)Math.Pow(10, (numberOfDigits - 14));
				}
			}

			if (DateTime.TryParseExact(dateAsLong.ToString(), hasTime ? "yyyyMMddHHmmss" : "yyyyMMdd",
			                           CultureInfo.InvariantCulture,
			                           DateTimeStyles.None, out DateTime dt))
			{
				return dt;
			}
		}

		return null;
	}
}
