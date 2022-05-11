package com.beamable.googlesignin;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.unity3d.player.UnityPlayer;

public class GoogleSignInActivity extends Activity {
    private static final int REQUEST_CODE_SIGNIN = 1;
    private static final String TAG = "GoogleSignInActivity";

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

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        try {
            final Intent intent = getIntent();
            _unityObject = intent.getStringExtra("unityObject");
            _unityMethod = intent.getStringExtra("unityMethod");
            GoogleSignInOptions options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                    .requestEmail()
                    .requestIdToken(intent.getStringExtra("clientId"))
                    .build();
            GoogleSignInClient client = GoogleSignIn.getClient(this, options);
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
        UnityPlayer.UnitySendMessage(_unityObject, _unityMethod, message);
    }

    public static class AccountNotFoundException extends Exception {}
}
