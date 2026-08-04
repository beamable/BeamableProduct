package com.beamable.googlesignin;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.unity3d.player.UnityPlayer;

/**
 * The plugin's entry point, and the only class name the C# side knows
 * ({@code Beamable.Platform.SDK.Auth.GoogleSignIn} hardcodes it for
 * {@code new AndroidJavaClass(...)}). It is both the proxy Activity that hosts the interactive
 * account chooser and the static facade for the paths that need no UI at all - those are
 * implemented in {@link GoogleSignInBridge}.
 *
 * <p>All responses travel back to Unity as a flat string through
 * {@code UnityPlayer.UnitySendMessage}. The vocabulary is a wire contract with the C# side; see
 * {@link GoogleSignInBridge} for the constants.
 */
public class GoogleSignInActivity extends Activity {
    private static final int REQUEST_CODE_SIGNIN = 1;
    private static final String TAG = "GoogleSignInActivity";

    // Dormant fallbacks: the intent extras are always supplied by login(), so these values are
    // never actually used. Kept as-is for backwards compatibility. Note "Behaviour" does not match
    // the C# class name (GoogleSignInBehavior), so this fallback would silently drop messages if it
    // ever did apply - another reason the newer entry points require the target to be passed in.
    private String _unityObject = "GoogleSignInBehaviour";
    private String _unityMethod = "GoogleAuthResponse";

    /**
     * Commence Google Sign-In login.
     *
     * Call this from Unity like this:
     *   var login = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");
     *   login.CallStatic("login", gameObject.name, "GoogleAuthResponse", ClientId);
     *
     * @param callbackObject name of the Unity GameObject to call back.
     * @param callbackMethod name of the callback method on the object.
     * @param clientId Google Cloud App client ID from web-app credentials.
     */
    public static void login(String callbackObject, String callbackMethod, String clientId) {
        final Context context = UnityPlayer.currentActivity.getApplicationContext();
        final Intent intent = new Intent(context, GoogleSignInActivity.class);

        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        intent.putExtra("unityObject", callbackObject);
        intent.putExtra("unityMethod", callbackMethod);
        intent.putExtra("clientId", clientId);
        context.startActivity(intent);
    }

    /**
     * Refresh the Google ID token of the account the player has already granted on this device,
     * without showing any UI.
     *
     * Call this from Unity like this:
     *   var login = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");
     *   login.CallStatic("silentLogin", gameObject.name, "GoogleAuthResponse", ClientId);
     *
     * Responds with the ID token on success, or "NO_CREDENTIAL - 4" when nobody has signed in on
     * this device yet or consent is required - a normal outcome, not an error. Unlike
     * {@link #login} this launches no Activity, shows no account chooser, and leaves the cached
     * account in place.
     *
     * @param callbackObject name of the Unity GameObject to call back.
     * @param callbackMethod name of the callback method on the object.
     * @param clientId Google Cloud App client ID from web-app credentials. Must match the value
     *                 passed to {@link #login}, or the cached credential will not be recognised.
     */
    public static void silentLogin(String callbackObject, String callbackMethod, String clientId) {
        GoogleSignInBridge.silentLogin(callbackObject, callbackMethod, clientId);
    }

    /**
     * Forget the cached Google account. This is what a game's "log out" button should call: without
     * it, a player who logs out is silently signed back into the same Google account on the next
     * launch and cannot hand the device to someone else.
     *
     * Call this from Unity like this:
     *   var login = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");
     *   login.CallStatic("signOut", gameObject.name, "GoogleSignOutResponse", ClientId);
     *
     * Responds with "SIGNED_OUT" on success. Does not withdraw the OAuth grant, so signing back in
     * does not re-prompt for consent.
     *
     * @param callbackObject name of the Unity GameObject to call back.
     * @param callbackMethod name of the callback method on the object.
     * @param clientId Google Cloud App client ID from web-app credentials.
     */
    public static void signOut(String callbackObject, String callbackMethod, String clientId) {
        GoogleSignInBridge.signOut(callbackObject, callbackMethod, clientId);
    }

    /**
     * Withdraw the OAuth grant entirely, responding with "REVOKED" on success.
     *
     * Call this from Unity like this:
     *   var login = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");
     *   login.CallStatic("revokeAccess", gameObject.name, "GoogleSignOutResponse", ClientId);
     *
     * DESTRUCTIVE: the player must pass through the full consent screen again next time. Wire this
     * to "delete account" or "unlink Google" only - use {@link #signOut} for an ordinary log out.
     *
     * @param callbackObject name of the Unity GameObject to call back.
     * @param callbackMethod name of the callback method on the object.
     * @param clientId Google Cloud App client ID from web-app credentials.
     */
    public static void revokeAccess(String callbackObject, String callbackMethod, String clientId) {
        GoogleSignInBridge.revokeAccess(callbackObject, callbackMethod, clientId);
    }

    /**
     * The plugin's own version, set from googlesignin/build.gradle.
     *
     * Call this from Unity like this:
     *   var login = new AndroidJavaClass("com.beamable.googlesignin.GoogleSignInActivity");
     *   var version = login.CallStatic&lt;string&gt;("getPluginVersion");
     *
     * The C# side uses this to detect a googlesignin-release.aar that predates a feature it wants
     * to use, and degrade gracefully instead of throwing on a missing method. Older .aar files have
     * no such method, so a failed call means "older than 2.0.0".
     */
    public static String getPluginVersion() {
        return BuildConfig.PLUGIN_VERSION;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        try {
            final Intent intent = getIntent();
            _unityObject = intent.getStringExtra("unityObject");
            _unityMethod = intent.getStringExtra("unityMethod");
            // Shared with the silent path on purpose - silentSignIn() only recognises the cached
            // account if the options match exactly. See GoogleSignInBridge#buildOptions.
            GoogleSignInClient client =
                    GoogleSignIn.getClient(this, GoogleSignInBridge.buildOptions(intent.getStringExtra("clientId")));
            // Force the account chooser to appear every time (beam-1880). Deliberately not awaited:
            // signOut() returns a Task, so this races getSignInIntent() below. It has worked in
            // practice since 2022 and is left alone rather than re-timed - but it IS a race, so if
            // account selection ever starts misbehaving, start here.
            // The silent path must never do this, which is why it does not share this method.
            client.signOut();
            startActivityForResult(client.getSignInIntent(), REQUEST_CODE_SIGNIN);
        } catch (Exception e) {
            Log.e(TAG, "Exception before sign-in", e);
            sendResponse("EXCEPTION - " + e.getLocalizedMessage());
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        Log.d(TAG, "onActivityResult: " + requestCode + " : " + resultCode);
        try {
            if (resultCode == RESULT_CANCELED) {
                Log.i(TAG, "Sign-in canceled");
                sendResponse("CANCELED");
            } else if (requestCode == REQUEST_CODE_SIGNIN) {
                Log.d(TAG, "Successful sign-in response");
                GoogleSignInAccount account = GoogleSignIn.getSignedInAccountFromIntent(data).getResult();
                if (account == null) {
                    throw new AccountNotFoundException();
                }
                String token = account.getIdToken();
                sendResponse(token == null ? "UNKNOWN" : token);
            } else {
                Log.w(TAG, "Sign-in response had unexpected request code: " + requestCode);
                sendResponse("UNKNOWN");
            }
        } catch (Exception e) {
            Log.e(TAG, "Exception during sign-in", e);
            sendResponse("EXCEPTION - " + e.getLocalizedMessage());
        } finally {
            finish();
        }
    }

    /**
     * Send a response back to Unity. Unity will receive this as soon as the activity is finished.
     * @param message message to send to the callback.
     */
    private void sendResponse(String message) {
        GoogleSignInBridge.sendResponse(_unityObject, _unityMethod, message);
    }

    public static class AccountNotFoundException extends Exception {}
}
