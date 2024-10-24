using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.Models
{
	[Serializable]
	public class UsamDataModel : IStorageHandler<UsamDataModel>, Beamable.Common.Dependencies.IServiceStorable
	{
		private StorageHandle<UsamDataModel> _saveHandle;
		[SerializeField] private List<MicroserviceVisualsModel> _visualModels = new List<MicroserviceVisualsModel>();
		public List<MicroserviceVisualsModel> VisualModels => _visualModels;

		public MicroserviceVisualsModel GetModel(string name)
		{
			var existingModel = VisualModels.FirstOrDefault(m => m._name == name);
			if (existingModel != null)
			{
				existingModel.Connect();
				return existingModel;
			}
			var model = new MicroserviceVisualsModel() { _name = name };

			model.Connect();
			VisualModels.Add(model);
			_saveHandle?.Save();
			return model;
		}

		public void ReceiveStorageHandle(StorageHandle<UsamDataModel> handle)
		{
			_saveHandle = handle;
		}

		public void OnBeforeSaveState()
		{
			foreach (var model in _visualModels)
			{
				model.Disconnect();
			}
		}

		public void OnAfterLoadState()
		{
			foreach (var model in _visualModels)
			{
				model.Connect();
			}
		}
	}
}
