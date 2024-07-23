using UnityEngine;
public class SteamProvider : MonoBehaviour
{
#if USE_STEAMWORKS
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
#endif

    public void Start()
    {
#if USE_STEAMWORKS
        if(SteamManager.Initialized)
        {
            string name = Steamworks.SteamFriends.GetPersonaName();
            var appId = Steamworks.SteamUtils.GetAppID();
            Debug.Log($"Steam User Name = {name}, Steam App ID = {appId.m_AppId}");
        }
#endif
    }
}
