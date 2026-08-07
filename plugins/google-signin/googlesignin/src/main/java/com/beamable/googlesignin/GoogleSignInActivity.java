package com.beamable.googlesignin;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

import java.util.concurrent.atomic.AtomicBoolean;

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
     * The client ID this attempt was started with. Kept so that a failure can report it, since it is
     * the single most useful thing to compare against the Google Cloud console.
     */
    private String _clientId;

    /**
     * Guards the single response this Activity is allowed to send.
     *
     * <p>Exactly-once settlement is a correctness requirement, not tidiness. Unity's side of an
     * interactive request ({@code GoogleSignInService.SignIn}) applies <em>no</em> timeout on
     * purpose - the account chooser can legitimately own the screen for minutes - so a request that
     * ends without a response leaves the C# promise, and any Account Management flow awaiting it,
     * pending for the rest of the process. Every exit from this Activity therefore goes through
     * {@link #settle}, including the {@link #onDestroy} backstop, and the first one wins.
     *
     * <p>Atomic rather than a plain boolean only as insurance: every writer here runs on the main
     * thread today, but a response sent twice is a bug Unity cannot see.
     */
    private final AtomicBoolean _settled = new AtomicBoolean(false);

    /** True once the pre-login {@code signOut()} has completed, successfully or not. */
    private boolean _signOutFinished;

    /** True once the chooser intent has been handed to {@code startActivityForResult}. */
    private boolean _chooserLaunched;

    /** True between {@code onResume} and {@code onPause}, i.e. while this Activity is foreground. */
    private boolean _resumed;

    /** Set at the top of {@link #onDestroy}, so {@link #settle} does not call {@code finish()} there. */
    private boolean _destroying;

    /** Built in {@code onCreate} and used again once the pre-login sign-out completes. */
    private GoogleSignInClient _client;

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
            _clientId = intent.getStringExtra("clientId");

            // Recorded on every attempt, not just on failure, so a log always says which credential
            // was used. A client ID is a public identifier - it ships inside the APK.
            Log.i(TAG, "Starting interactive sign-in with clientId="
                    + (_clientId == null || _clientId.length() == 0 ? "(none)" : _clientId));

            // Shared with the silent path on purpose - silentSignIn() only recognises the cached
            // account if the options match exactly. See GoogleSignInBridge#buildOptions.
            _client = GoogleSignIn.getClient(this, GoogleSignInBridge.buildOptions(_clientId));

            // Force the account chooser to appear every time (beam-1880). Awaited, rather than raced
            // against getSignInIntent() as it used to be: signOut() completing underneath a running
            // SignInHubActivity was this file's own documented first suspect whenever account
            // selection misbehaved, and there is no reason to keep the race when the fix is one
            // listener. The silent path must never sign out, which is why it does not share this
            // method.
            //
            // Deliberately NOT addOnCompleteListener(this, ...). An Activity-scoped listener is
            // removed in onStop and never re-added, so a player who backgrounds the app in the
            // window between onCreate and signOut() completing loses the callback permanently: no
            // chooser is launched, no response reaches Unity, and because SignIn() has no timeout
            // the promise stays pending forever. The unscoped listener always runs; deciding
            // whether it is safe to act on it is #launchChooserWhenReady's job, and onDestroy is
            // the backstop for the case where it never becomes safe.
            _client.signOut().addOnCompleteListener(new OnCompleteListener<Void>() {
                @Override
                public void onComplete(Task<Void> task) {
                    // GMS dispatches an executor-less listener on the main looper, which is the same
                    // thread as every lifecycle callback here - so the flags below need no locking.
                    _signOutFinished = true;
                    launchChooserWhenReady();
                }
            });
        } catch (Throwable t) {
            Log.e(TAG, "Exception before sign-in", t);
            settle(GoogleSignInBridge.describeThrowable(t));
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        _resumed = true;

        // Normally a no-op: the sign-out listener cannot run before this, because onCreate through
        // onResume are one message on the main looper. It matters on the way back from being
        // backgrounded, where it is what finally launches a chooser that was deferred.
        launchChooserWhenReady();
    }

    @Override
    protected void onPause() {
        _resumed = false;
        super.onPause();
    }

    @Override
    protected void onDestroy() {
        _destroying = true;

        // The backstop that makes settlement exactly-once rather than at-most-once. Reached without
        // a response whenever the OS tears this Activity down mid-flight: destroyed while a
        // deferred chooser was still waiting for the player to return, or reclaimed while the
        // chooser itself was in front, in which case onActivityResult will never be delivered.
        // Either way this request has no other way to end, so Unity has to hear something.
        //
        // Cancelled, not an error: the player leaving is exactly what this means, it is how
        // GoogleSignInBridge#describeInteractiveResult already reports a torn-down Activity, and it
        // lands on the C# side as a benign GoogleSignInStatus.Cancelled rather than something a
        // game would show an error dialog for.
        //
        // A configuration change is not a teardown - onCreate runs again on the new instance and
        // restarts the flow - so answering here would settle a request that is still live.
        if (!isChangingConfigurations()) {
            settle(GoogleSignInBridge.RESPONSE_CANCELED + " - the sign-in Activity was destroyed before "
                    + "sign-in finished");
        }

        super.onDestroy();
    }

    /**
     * Launch the account chooser, once the pre-login sign-out has finished and this Activity is
     * actually in the foreground.
     *
     * <p>Both conditions have to hold. Starting an Activity from a stopped one is subject to
     * Android 10's background-activity-start restrictions and would be silently dropped, which is
     * the same indefinite hang this method exists to prevent - so if the player has backgrounded
     * the app, the launch waits for {@link #onResume} instead. If they never come back,
     * {@link #onDestroy} settles the request.
     *
     * <p>Safe to call more than once, from either trigger, in any order.
     */
    private void launchChooserWhenReady() {
        if (_chooserLaunched || _settled.get() || !_signOutFinished) {
            return;
        }

        if (!_resumed || isFinishing()) {
            Log.i(TAG, "Pre-login sign-out finished while the Activity was not in the foreground; "
                    + "deferring the account chooser until the player returns");
            return;
        }

        _chooserLaunched = true;

        try {
            startActivityForResult(_client.getSignInIntent(), REQUEST_CODE_SIGNIN);
        } catch (Throwable t) {
            Log.e(TAG, "Could not start the account chooser", t);
            settle(GoogleSignInBridge.describeThrowable(t));
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        // hasData distinguishes "GMS answered with a Status" from "the Activity was torn down", which
        // is the difference between a diagnosable failure and a genuine dismissal.
        Log.d(TAG, "onActivityResult: requestCode=" + requestCode + " resultCode=" + resultCode
                + " hasData=" + (data != null));
        try {
            if (requestCode != REQUEST_CODE_SIGNIN) {
                Log.w(TAG, "Sign-in response had unexpected request code: " + requestCode);
                settle(GoogleSignInBridge.RESPONSE_UNKNOWN);
                return;
            }

            // Note there is deliberately no resultCode check. GMS reports RESULT_CANCELED both for a
            // dismissal and for a configuration rejection; only the Status inside the intent tells
            // them apart, and this used to collapse both into "CANCELED".
            settle(GoogleSignInBridge.describeInteractiveResult(this, data, resultCode, _clientId));
        } catch (Throwable t) {
            Log.e(TAG, "Exception during sign-in", t);
            settle(GoogleSignInBridge.describeThrowable(t));
        }
    }

    /**
     * End this request: send exactly one response back to Unity and finish the Activity.
     *
     * <p>Every exit path calls this, and only the first call does anything - see {@link #_settled}
     * for why that matters. Later calls, including the {@link #onDestroy} backstop firing after a
     * normal response, are no-ops, so the more specific outcome always wins.
     *
     * <p>{@code UnitySendMessage} queues for the player loop, so the C# callback runs on a later
     * frame regardless of whether the Activity has finished by then.
     *
     * @param message message to send to the callback.
     */
    private void settle(String message) {
        if (!_settled.compareAndSet(false, true)) {
            Log.d(TAG, "Ignoring a second response for the same request: " + message);
            return;
        }

        GoogleSignInBridge.sendResponse(_unityObject, _unityMethod, message);

        if (!_destroying && !isFinishing()) {
            finish();
        }
    }

    /**
     * No longer thrown - {@link GoogleSignInBridge#describeInteractiveResult} reports a missing
     * account as {@code UNKNOWN} rather than as an exception, so that the response carries the GMS
     * status instead of a Java class name. Retained because it is public API.
     */
    public static class AccountNotFoundException extends Exception {}
}
