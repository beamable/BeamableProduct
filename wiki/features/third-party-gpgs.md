# Google Play Game Services - Third Party Authorization Provider

This guide shows how to use Google Play Game Services as a authorization provider for Beamable.

## Getting ready

Make sure you have the Google Play Game Services SDK installed and configured in your project.
Useful links:
- [Google Play Game Services SDK Repository](https://github.com/playgameservices/play-games-plugin-for-unity)
- [Google Play Game Services SDK Unity Setup Guide](https://developer.android.com/games/pgs/unity/unity-start)
- [Configuring Server Access to Google Play Game Services](https://developer.android.com/games/pgs/android/server-access)

> ### GPGS Configuration Requirements for integration
> You will need both Web application credentials AND platform specific credentials for Android.

## Integration

Once you have completed the GPGS setup, you can integrate Google Play Game Services into your Beamable project.

### Enabling Integration

If you have Android platform enabled you can attempt to enable integration. Do it by clicking the `Window/Beamable/Utilities/Third Party Integration Helpers/[Google Play Games] Enable` menu item in Unity Editor.
It will check if the GPGS SDK is installed and configuration has the Web ID specified.

Now is time to go to the Google Cloud Portal and download the client secret file for Web application specified earlier in the configuration.

Then you can click the `Window/Beamable/Utilities/Third Party Integration Helpers/[Google Play Games] Update Realm config from web client secret file` menu item in Unity Editor. It will ask you to select the downloaded file. Next it will check if file is valid and the web id matches the one specified in the GPGS SDK configuration. After that it will convert the value to the format expected by Beamable Realm config and it will update the realm config `auth|gps_secret` value.

Once that succeeds we can proceed to the next step.

### Login code

Here is sample code that on Start will attempt to either register third party id or restore the account with the id and switch to it.
```csharp
using Beamable;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class GooglePlayLogin : MonoBehaviour
{
    async void Start()
    {
        await AttemptGooglePlayLogin();
    }

    public async Promise<bool> AttemptGooglePlayLogin()
    {
        var beamContext = await BeamContext.Default.Instance;
        var localLogin = await SignInWithGPG.PerformLocalLogin();
        if (localLogin != SignInStatus.Success)
        {
            Debug.Log($"FAILED: {localLogin}");
            return false;
        }

        var serverCode = await SignInWithGPG.RequestServerSideToken();
        if (string.IsNullOrWhiteSpace(serverCode))
        {
            Debug.Log("[BEAMABLE LOGIN] FAILED to obtain server side token");
            return false;
        }

        bool shouldRestoreAccount = beamContext.Accounts.Current.HasThirdParty(AuthThirdParty.GoogleGamesServices);
        try
        {
            if(!shouldRestoreAccount)
            {
                _ = await beamContext.Api.AuthService.RegisterThirdPartyCredentials(AuthThirdParty.GoogleGamesServices,
                    serverCode);
                await beamContext.Accounts.Refresh();
                return true;
            }
        }
        catch (PlatformRequesterException e)
        {
            Debug.Log($"[BEAMABLE LOGIN] Google Play login failed: {e.Message}, {JsonUtility.ToJson(e.Error)}");

            shouldRestoreAccount = e.Error.error.Equals("ThirdPartyAssociationAlreadyInUseError");
        }

        if (!shouldRestoreAccount)
        {
            return false;
        }

        try
        {
            serverCode = await SignInWithGPG.RequestServerSideToken();
            if (string.IsNullOrWhiteSpace(serverCode))
            {
                Debug.Log("[BEAMABLE LOGIN] FAILED to obtain server side token");
                return false;
            }
            var newToken =
                await beamContext.Api.AuthService.LoginThirdParty(AuthThirdParty.GoogleGamesServices, serverCode,
                    false);
            await beamContext.ChangeAuthorizedPlayer(newToken);
            return true;
        }
        catch (PlatformRequesterException e)
        {
            Debug.Log($"[BEAMABLE LOGIN] FAILED TO SWITCHED TO ACCOUNT WITH GPGS: {JsonUtility.ToJson(e.Error)}");
        }

        return false;
    }
}
```
