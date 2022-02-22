using Beamable.Common.Content;
using Beamable.Common.Inventory;

namespace BeamableEditor.Tests.Platform.Inventory
{
	[ContentType(InventoryTestItem.CONTENT)]
	public class InventoryTestItem : ItemContent
	{
		public const string CONTENT = "inventoryTestItem";
		public const string FULL_CONTENT_ID = "items." + CONTENT;
		public int Foo;
	}

	[System.Serializable]
	public class InventoryTestItemRef : ItemRef<InventoryTestItem>
	{
		public InventoryTestItemRef()
		{

		}
		public InventoryTestItemRef(string id)
		{
			Id = id;
		}
	}
}
