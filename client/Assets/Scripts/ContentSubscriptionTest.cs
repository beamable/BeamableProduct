using Beamable;
using Beamable.Common.Inventory;
using UnityEngine;

public class ContentSubscriptionTest : MonoBehaviour
{
    public CurrencyRef CurrencyRef;


    // Start is called before the first frame update
    async void Start()
    {
        var b = await Beamable.API.Instance;
        string lastVersion = null;
        CurrencyContent firstContent = null;
        b.ContentService.Subscribe(async manifest =>
        {
            var content = await CurrencyRef.Resolve();

            if (lastVersion == null)
            {
                firstContent = content;
                lastVersion = content.Version;
                Debug.Log($"Got initial version of content. Make an update to the {CurrencyRef.Id} and publish the manifest. Remember to focus the Game Window, and wait a few seconds for the manifest update to arrive...");
            }
            else if (string.Equals(lastVersion,content.Version))
            {
                // TestHandle.Fail("didn't change :( ")
                Debug.Log($"{CurrencyRef.Id} didn't change! Test failure!");
                Debug.Log("Initial content. " + content.ToJson());
                Debug.Log("Latest content. " + firstContent.ToJson());
            }
            else
            {
                Debug.Log($"{CurrencyRef.Id} changed, Test Pass!!");
            }
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
