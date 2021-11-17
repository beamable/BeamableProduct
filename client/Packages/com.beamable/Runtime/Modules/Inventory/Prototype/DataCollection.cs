using System;
using System.Collections.Generic;

namespace Modules.Inventory.Prototype
{
	public abstract class DataCollection<T> : List<T> where T : class
	{
		protected Action CollectionUpdated
		{
			get;
		}

		protected abstract void Subscribe();
		public abstract void Unsubscribe();

		protected DataCollection(Action onCollectionUpdated)
		{
			CollectionUpdated = onCollectionUpdated;
			Subscribe();
		}
	}
}
