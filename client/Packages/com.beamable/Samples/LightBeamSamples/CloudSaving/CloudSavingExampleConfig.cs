using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Beamable/Examples/CloudSaving Example Config")]
public class CloudSavingExampleConfig : ScriptableObject
{
	public HomePage homePage;
	public StringPage stringPage;
	public BytePage bytePage;
	public JsonPage jsonPage;
}
