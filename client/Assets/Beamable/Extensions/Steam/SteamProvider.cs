using UnityEngine;
using Beamable.Service;
using Beamable.Common.Steam;

public class SteamProvider : MonoBehaviour
{
#if USE_STEAMWORKS
    private void Awake()
    {
        ServiceManager.ProvideWithDefaultContainer<ISteamService>(new SteamService());
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
