using Beamable.Editor.UI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Toolbox.Models
{
	public class ToolboxMockData : IWidgetSource
	{
		public ToolboxMockData()
		{
			this.Count = 10;

			//TODO: Create hard coded widgets individually following the data from toolbox data
			for(int i = 0; i < this.Count; i++)
			{
				Widget widget = new Widget();
				widget.Name = "fake widget tool";
				widget.Description = "fake description";
				widget.OrientationSupport = 1 & 2;
				widget.Tags = WidgetTags.FLOW & WidgetTags.COMPONENT;
				widget.Icon = new Texture2D(128, 128);
				widget.Prefab = new GameObject();

				this.Widgets.Add(widget);

			}
			
		}

		
		public List<Widget> Widgets = new List<Widget>();

		public int Count { get; set; }

		public Widget Get(int index)
		{
			return Widgets[index];
		}
	}
}

