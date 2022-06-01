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

			Widget[] w = { 
				new Widget("Admin Flow", "UI for game commands and cheats", 1 & 2, WidgetTags.FLOW & WidgetTags.ADMIN, new Texture2D(128, 128), new GameObject()),
				new Widget("Currency HUD", "UI for virtual currency", 1 & 2, WidgetTags.COMPONENT & WidgetTags.SHOP & WidgetTags.INVENTORY & WidgetTags.CURRENCY, new Texture2D(128, 128), new GameObject()),
				new Widget("Account HUD", "UI to open login flow", 1 & 2, WidgetTags.COMPONENT & WidgetTags.ACCOUNTS, new Texture2D(128, 128), new GameObject()),
				new Widget("Account Management Flow", "Allows users to manage account", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.ACCOUNTS, new Texture2D(128, 128), new GameObject()),
				new Widget("Leaderboard Flow", "Allow user to manage leaderboard", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.LEADERBOARDS, new Texture2D(128, 128), new GameObject()),
				new Widget("LeaderBoard Flow (NEW)", "Allow user to manage leaderboard", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.LEADERBOARDS, new Texture2D(128, 128), new GameObject()),
				new Widget("Announcement Flow", "Allow user to manage announcements", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.INVENTORY & WidgetTags.CURRENCY, new Texture2D(128, 128), new GameObject()),
				new Widget("Inventory Flow", "Allow user to manage inventory", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.ADMIN, new Texture2D(128, 128), new GameObject()),
				new Widget("Tournament Flow", "Allow user set up a recurring tournament", WidgetOrientationSupport.LANDSCAPE, WidgetTags.FLOW & WidgetTags.LEADERBOARDS, new Texture2D(128, 128), new GameObject()),
				new Widget("Store Flow", "Allow user to shop", WidgetOrientationSupport.PORTRAIT, WidgetTags.FLOW & WidgetTags.SHOP, new Texture2D(128, 128), new GameObject())
			};

			//TODO: Create hard coded widgets individually following the data from toolbox data
			Widgets = w.ToList();

		}

		
		public List<Widget> Widgets = new List<Widget>();

		public int Count { get; set; }

		public Widget Get(int index)
		{
			return Widgets[index];
		}

		Models.Widget IWidgetSource.Get(int index)
		{
			throw new NotImplementedException();
		}

		public class Widget
		{
			public Widget(string name, string desc, WidgetOrientationSupport orientation, WidgetTags tags, Texture icon, GameObject prefab)
			{
				this.Name = name;
				this.Description = desc;
				this.OrientationSupport = orientation;
				this.Tags = tags;
				this.Icon = icon;
				this.Prefab = prefab;
			}

			public string Name;
			public string Description;
			[EnumFlags]
			public WidgetOrientationSupport OrientationSupport;
			[EnumFlags]
			public WidgetTags Tags;
			public Texture Icon;
			public GameObject Prefab;
		}
	}
}

