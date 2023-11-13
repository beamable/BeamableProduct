using Beamable.Editor.UI.Model;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Models
{
	public interface IMicroserviceVisualsModel
	{
		bool AreLogsAttached { get; }
		float ElementHeight { get; set; }
		bool IsSelected { get; set; }
		bool IsCollapsed { get; set; }
		
		Action OnLogsDetached { get; set; }
		Action OnLogsAttached { get; set; }
		Action<bool> OnLogsAttachmentChanged { get; set; }
		Action<bool> OnSelectionChanged { get; set; }
		Action OnSortChanged { get; set; }
		LogMessageStore Logs { get; }
		
		void DetachLogs();
		void AttachLogs();
		void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
	}
	
	[Serializable]
	public class MicroserviceVisualsModel : IMicroserviceVisualsModel
	{
		private const float DEFAULT_HEIGHT = 300.0f;

		public bool AreLogsAttached { get; set; }
		
		public float ElementHeight
		{
			get => _visualHeight;
			set => _visualHeight = value;
		}
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnSelectionChanged?.Invoke(value);
			}
		}
		public bool IsCollapsed
		{
			get => _isCollapsed;
			set => _isCollapsed = value;
		}
		
		public Action OnLogsDetached { get; set; }
		public Action OnLogsAttached { get; set; }
		public Action<bool> OnLogsAttachmentChanged { get; set; }
		public Action<bool> OnSelectionChanged { get; set; }
		public Action OnSortChanged { get; set; }
		public LogMessageStore Logs => _logs;
		
		[SerializeField] private LogMessageStore _logs = new LogMessageStore();
		[SerializeField] private bool _isSelected;
		[SerializeField] private bool _isCollapsed = false;
		[SerializeField] private float _visualHeight = DEFAULT_HEIGHT;
		[SerializeField]public string name;

		public static MicroserviceVisualsModel GetModel(string name)
		{
			MicroserviceVisualsModel model;
			try
			{
				var key = GetKey(name);
				if (EditorPrefs.HasKey(key))
				{
					string json = EditorPrefs.GetString(key, string.Empty);
					model = JsonUtility.FromJson<MicroserviceVisualsModel>(json);
					if (model != null)
					{
						return model;
					}
				}
				
			}catch
			{
				//
			}
			model = new MicroserviceVisualsModel() {name = name};
			return model;
		}

		public void Save()
		{
			string json = JsonUtility.ToJson(this);
			EditorPrefs.SetString(GetKey(name), json);
		}
		
		public void DetachLogs()
		{
			if (!AreLogsAttached) return;

			AreLogsAttached = false;
			OnLogsDetached?.Invoke();
			OnLogsAttachmentChanged?.Invoke(false);
		}
		public void AttachLogs()
		{
			if (AreLogsAttached) return;
			AreLogsAttached = true;
			OnLogsAttached?.Invoke();
			OnLogsAttachmentChanged?.Invoke(true);
		}

		public virtual void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
		{
		}

		private static string GetKey(string name) => $"Beamable{nameof(MicroserviceVisualsModel)}{name}";
	}
}
