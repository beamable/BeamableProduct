using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Realms;
using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;

namespace tests;

public class UnitySerializationContractTests
{
	[Test]
	public void RealmGraphRebuildsParentsWithoutSerializingBackReferences()
	{
		var projects = new List<ProjectViewDTO>
		{
			new ProjectViewDTO { pid = "root", projectName = "Production" },
			new ProjectViewDTO { pid = "child", projectName = "Staging", parent = "root" }
		};
		var realms = RealmsService.ProcessProjects("customer", projects);
		var root = realms.Single(r => r.Pid == "root");
		var child = realms.Single(r => r.Pid == "child");
		Assert.That(child.Parent, Is.SameAs(root));
		Assert.That(child.FindRoot(), Is.SameAs(root));
		Assert.That(child.Depth, Is.EqualTo(1));
		var json = Json.Serialize(root, new StringBuilder());
		StringAssert.Contains("\"Pid\":\"child\"", json);
		StringAssert.DoesNotContain("\"Parent\"", json);
		var rebuilt = RealmsService.ProcessProjects("customer", projects);
		Assert.That(rebuilt.Single(r => r.Pid == "child").FindRoot().Pid, Is.EqualTo("root"));
	}

	[Test]
	public void InventoryJsonKeepsDictionariesAndNestedItemProperties()
	{
		var view = new InventoryView();
		view.currencies["currency.coins"] = 42;
		view.items["items.weapon"] = new List<ItemView>
		{
			new ItemView { id = 7, properties = new Dictionary<string, string> { ["name"] = "Sword" } }
		};
		var json = Json.Serialize(view, new StringBuilder());
		var restored = Json.Deserialize<InventoryView>(json);
		Assert.That(restored.currencies["currency.coins"], Is.EqualTo(42));
		Assert.That(restored.items["items.weapon"][0].id, Is.EqualTo(7));
		Assert.That(restored.items["items.weapon"][0].properties["name"], Is.EqualTo("Sword"));
	}
}
