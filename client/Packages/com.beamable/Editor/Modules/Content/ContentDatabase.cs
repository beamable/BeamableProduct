using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	public struct ContentDatabaseEntry
	{
		public string assetGuid;
		public string assetPath;
		public string contentId;
		public string contentName;
		public string contentType;
	}

	public class ContentDatabseMenus
	{
		public static ContentDatabase db = new ContentDatabase();
		[MenuItem("Content/Index")]
		public static void Index()
		{
			db.Index();
		}
		
		[MenuItem("Content/GetCurrencies")]
		public static void GetCurrencie()
		{
			var content = db.GetContent<CurrencyContent>();
			foreach (var c in content)
			{
				Debug.Log("Found: " + c.contentId);
			}
		}
	}
	
	public class ContentDatabase
	{
		private ContentTypeReflectionCache _typeCache;
		private List<ContentDatabaseEntry> _data;

		private Dictionary<string, List<ContentDatabaseEntry>> _typeToExactContent =
			new Dictionary<string, List<ContentDatabaseEntry>>();

		private Dictionary<string, List<ContentDatabaseEntry>> _typeToAssignableContent =
			new Dictionary<string, List<ContentDatabaseEntry>>();

		public ContentDatabase()
		{
			_typeCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
		}
		
		public void Index()
		{
			var root = Constants.Directories.DATA_DIR;
			var toExpand = new Stack<string>();
			var typeString = new Stack<string>();
			var typeToContentList = new Stack<List<ContentDatabaseEntry>>();
			var typeToContentAssignableList = new Stack<List<ContentDatabaseEntry>>();
			toExpand.Push(root);

			_data = new List<ContentDatabaseEntry>();

			string prev = null;
			string curr = null;
			string prevType = null;
			string currType = null;
			List<ContentDatabaseEntry> currList = null;
			List<ContentDatabaseEntry> prevList = null;
			List<ContentDatabaseEntry> currAssignableList = null;
			List<ContentDatabaseEntry> prevAssignableList = null;
			
			while (toExpand.Count > 0)
			{
				prev = curr;
				curr = toExpand.Pop();

				var isChildOfPrev = prev != null && curr.StartsWith(prev);
				prevType = currType;
				var hasCurrType = typeString.TryPop(out currType);
				prevList = currList;
				var hasContentList = typeToContentList.TryPop(out currList);

				prevAssignableList = currAssignableList;
				typeToContentAssignableList.TryPop(out currAssignableList);
				
				foreach (var file in Directory.GetFiles(curr, "*.asset"))
				{
					var instance = new ContentDatabaseEntry();
					var name = file.Substring(curr.Length + 1, file.Length - (curr.Length + ".asset".Length + 1));
					
					instance.contentName = name;
					instance.assetPath = file;
					instance.contentType = currType;
					instance.contentId = currType + "." + name;
					// instance.assetGuid = AssetDatabase.AssetPathToGUID()
					_data.Add(instance);
					
					currList.Add(instance);
					// if (isChildOfPrev)
					{
						currAssignableList.Add(instance);
					}
				}
				
				// everything that we just created, needs to get added to the parent directory?
				
				foreach (var path in Directory.GetDirectories(curr))
				{
					var type = path.Substring(curr.Length + 1, path.Length - (curr.Length + 1));
					if (hasCurrType)
					{
						type = currType + "." + type;
					}
					typeString.Push(type);
					toExpand.Push(path);

					var nextContentList = new List<ContentDatabaseEntry>();
					typeToContentList.Push(nextContentList);
					_typeToExactContent[type] = nextContentList;
					
					// if (isChildOfPrev)
					var nextEntireList = currAssignableList == null ? new List<ContentDatabaseEntry>() : currAssignableList.ToList();

					// TODO: we are moving into a sub type, by definition. So we need to clone an array.
					typeToContentAssignableList.Push(nextEntireList);
					_typeToAssignableContent[type] = nextEntireList;
				}
			}

			// precompute all of the parent/child relationships between content for fast access later. 
			
			
			foreach (var content in _data)
			{
				Debug.Log($"type=[{content.contentType}] id=[{content.contentId}] name=[{content.contentName}] path=[{content.assetPath}]");
			}
		}

		public IReadOnlyList<ContentDatabaseEntry> GetContent<T>() where T : ContentObject
		{
			var name = _typeCache.TypeToName(typeof(T));
	
			
			
			Debug.Log("DSF:" + name);
			return _data.GetRange(0, 2);
		}

	}
}
