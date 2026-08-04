package com.beamable.googlesignin;

import android.app.Activity;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.auth.api.signin.GoogleSignInStatusCodes;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.common.api.CommonStatusCodes;
import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

/**
 * Implementation behind the static entry points on {@link GoogleSignInActivity}.
 *
 * <p>Everything here runs <em>without</em> an Activity. {@code silentSignIn}, {@code signOut} and
 * {@code revokeAccess} only need a {@link android.content.Context} and return a
 * {@link Task}, so hosting them in the proxy Activity would be all cost and no benefit: it would
 * background the Unity player (pausing the player loop, which also delays delivery of the response
 * until the proxy finishes), flash a blank window, and add a lifecycle window in which the OS could
 * kill the Activity mid-flight. A silent sign-in that visibly interrupts the game is not silent.
 *
 * <p>Two rules that are load-bearing for anyone editing this file:
 * <ul>
 *   <li><b>No lambdas or method references.</b> At {@code -target 8} javac compiles them to
 *       {@code invokedynamic} + {@code LambdaMetafactory}, which relies on the <em>consuming</em>
 *       project's desugaring being enabled. Anonymous inner classes have no such dependency.</li>
 *   <li><b>Catch {@link Throwable}, not {@link Exception}.</b> A consuming project that has not
 *       added {@code play-services-auth} to its {@code mainTemplate.gradle} raises
 *       {@link NoClassDefFoundError}, which is an {@link Error}. Caught as a {@code Throwable} it
 *       is reported to Unity as a normal failure; uncaught it escapes the JNI boundary and takes
 *       the player down. For the same reason every public entry point takes and returns only
 *       {@link String}: a GMS type in a signature would move the failure to method resolution,
 *       where it cannot be caught.</li>
 * </ul>
 */
final class GoogleSignInBridge {
	private static final String TAG = "GoogleSignInActivity";

	/** Response vocabulary. These strings are a wire contract with the C# side - see
	 * {@code Beamable.Platform.SDK.Auth.GoogleSignInResult} - and must not be reworded. */
	static final String RESPONSE_CANCELED = "CANCELED";
	static final String RESPONSE_UNKNOWN = "UNKNOWN";
	static final String RESPONSE_NO_CREDENTIAL = "NO_CREDENTIAL";
	static final String RESPONSE_SIGNED_OUT = "SIGNED_OUT";
	static final String RESPONSE_REVOKED = "REVOKED";
	static final String RESPONSE_EXCEPTION_PREFIX = "EXCEPTION - ";

	private GoogleSignInBridge() {
	}

	/**
	 * The single source of truth for the sign-in options.
	 *
	 * <p>This is shared with the interactive path in {@link GoogleSignInActivity} as a correctness
	 * requirement rather than for tidiness: {@code silentSignIn()} fails with
	 * {@code SIGN_IN_REQUIRED} unless the options match those the cached account was originally
	 * signed in with. If the two paths ever build their options separately, silent sign-in starts
	 * failing for reasons that look like a credential problem.
	 */
	static GoogleSignInOptions buildOptions(String clientId) {
		return new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
				.requestEmail()
				.requestIdToken(clientId)
				.build();
	}

	/**
	 * Refresh the signed-in player's Google ID token without showing any UI.
	 *
	 * <p>Deliberately does <em>not</em> call {@code signOut()} first, unlike
	 * {@link GoogleSignInActivity#login}, which force-signs-out so the account chooser always
	 * appears. Preserving the cached account is the entire point of this call.
	 *
	 * <p>Prefer this to {@code GoogleSignIn.getLastSignedInAccount()}: that returns a cached
	 * account whose ID token may already have expired, whereas {@code silentSignIn()} refreshes it.
	 */
	static void silentLogin(final String unityObject, final String unityMethod, final String clientId) {
		postToUiThread(unityObject, unityMethod, new Runnable() {
			@Override
			public void run() {
				final GoogleSignInClient client =
						GoogleSignIn.getClient(UnityPlayer.currentActivity, buildOptions(clientId));

				client.silentSignIn().addOnCompleteListener(new OnCompleteListener<GoogleSignInAccount>() {
					@Override
					public void onComplete(Task<GoogleSignInAccount> task) {
						sendResponse(unityObject, unityMethod, describeSilentResult(task));
					}
				});
			}
		});
	}

	/**
	 * Forget the cached Google account, so the next {@code silentLogin} reports
	 * {@link #RESPONSE_NO_CREDENTIAL} and the next interactive {@code login} starts clean.
	 *
	 * <p>This is what a game's "log out" button should call. It does not withdraw the OAuth grant,
	 * so signing back in does not re-prompt for consent.
	 */
	static void signOut(final String unityObject, final String unityMethod, final String clientId) {
		postToUiThread(unityObject, unityMethod, new Runnable() {
			@Override
			public void run() {
				final GoogleSignInClient client =
						GoogleSignIn.getClient(UnityPlayer.currentActivity, buildOptions(clientId));

				client.signOut().addOnCompleteListener(new OnCompleteListener<Void>() {
					@Override
					public void onComplete(Task<Void> task) {
						sendResponse(unityObject, unityMethod, describeVoidResult(task, RESPONSE_SIGNED_OUT));
					}
				});
			}
		});
	}

	/**
	 * Withdraw the OAuth grant entirely.
	 *
	 * <p><b>Destructive.</b> The player must pass through the full consent screen again next time.
	 * Wire this to "delete account" or "unlink Google", never to a plain "log out" button - use
	 * {@link #signOut} for that.
	 */
	static void revokeAccess(final String unityObject, final String unityMethod, final String clientId) {
		postToUiThread(unityObject, unityMethod, new Runnable() {
			@Override
			public void run() {
				final GoogleSignInClient client =
						GoogleSignIn.getClient(UnityPlayer.currentActivity, buildOptions(clientId));

				client.revokeAccess().addOnCompleteListener(new OnCompleteListener<Void>() {
					@Override
					public void onComplete(Task<Void> task) {
						sendResponse(unityObject, unityMethod, describeVoidResult(task, RESPONSE_REVOKED));
					}
				});
			}
		});
	}

	/**
	 * Post GMS work to the Android UI thread, reporting any failure to Unity instead of letting it
	 * escape into JNI.
	 *
	 * <p>Unity calls in on the player thread ("UnityMain"), which has no {@link android.os.Looper}.
	 * GMS is designed to cope with that, but hopping to the UI thread keeps this plugin on the same
	 * thread the interactive path has always used, and means the {@code Task} completion listener
	 * (whose default executor is the main looper) runs there too - so each request is
	 * single-threaded end to end.
	 *
	 * <p>It also removes any question of re-entrancy on the fast path where
	 * {@code silentSignIn()} returns an already-completed {@code Task}: the work is posted, so it
	 * has not started when the JNI call returns. Independently,
	 * {@code UnityPlayer.UnitySendMessage} queues for the player loop, so the C# callback always
	 * lands on a later frame and can never re-enter the caller.
	 */
	private static void postToUiThread(final String unityObject, final String unityMethod, final Runnable work) {
		try {
			final Activity activity = UnityPlayer.currentActivity;
			if (activity == null) {
				sendResponse(unityObject, unityMethod,
						RESPONSE_EXCEPTION_PREFIX + "UnityPlayer.currentActivity was null");
				return;
			}

			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					try {
						work.run();
					} catch (Throwable t) {
						Log.e(TAG, "Google Sign-In request failed", t);
						sendResponse(unityObject, unityMethod, describeThrowable(t));
					}
				}
			});
		} catch (Throwable t) {
			Log.e(TAG, "Could not dispatch Google Sign-In request", t);
			sendResponse(unityObject, unityMethod, describeThrowable(t));
		}
	}

	/** Classify a completed {@code silentSignIn()} task into the response vocabulary. */
	private static String describeSilentResult(Task<GoogleSignInAccount> task) {
		if (task.isSuccessful()) {
			final GoogleSignInAccount account = task.getResult();
			if (account == null) {
				Log.w(TAG, "Silent sign-in succeeded with no account");
				return RESPONSE_UNKNOWN;
			}

			final String idToken = account.getIdToken();
			if (idToken == null) {
				// Almost always a client ID that was not configured as a *web* OAuth client.
				Log.w(TAG, "Silent sign-in succeeded but no ID token was granted");
				return RESPONSE_UNKNOWN;
			}

			Log.d(TAG, "Silent sign-in succeeded");
			return idToken;
		}

		final Exception failure = task.getException();
		if (failure instanceof ApiException) {
			final int statusCode = ((ApiException) failure).getStatusCode();

			// SIGN_IN_REQUIRED is the ordinary "nobody has signed in on this device yet, or consent
			// is needed" outcome. It is a normal result for a silent attempt, not an error, and the
			// C# layer surfaces it as GoogleSignInStatus.NoCredential so the game can fall back to
			// showing a Google button.
			if (statusCode == CommonStatusCodes.SIGN_IN_REQUIRED) {
				Log.i(TAG, "Silent sign-in found no usable credential");
				return RESPONSE_NO_CREDENTIAL + " - " + statusCode;
			}

			return describeStatusCode("Silent sign-in failed", statusCode);
		}

		if (failure == null) {
			Log.w(TAG, "Silent sign-in failed with no exception");
			return RESPONSE_UNKNOWN;
		}

		Log.e(TAG, "Silent sign-in failed", failure);
		return describeThrowable(failure);
	}

	/** Classify a completed {@code signOut()} / {@code revokeAccess()} task. */
	private static String describeVoidResult(Task<Void> task, String successResponse) {
		if (task.isSuccessful()) {
			Log.d(TAG, successResponse);
			return successResponse;
		}

		final Exception failure = task.getException();
		if (failure instanceof ApiException) {
			return describeStatusCode("Request failed", ((ApiException) failure).getStatusCode());
		}

		if (failure == null) {
			return RESPONSE_UNKNOWN;
		}

		Log.e(TAG, "Request failed", failure);
		return describeThrowable(failure);
	}

	/**
	 * Keep the numeric status code in the message. This is what makes the difference between a
	 * support ticket that reads "EXCEPTION - 10" and one that reads
	 * "EXCEPTION - DEVELOPER_ERROR(10)" - code 10 is specifically the signal that the SHA-1 or the
	 * OAuth client ID is misconfigured, which is the single most common setup mistake.
	 */
	private static String describeStatusCode(String logPrefix, int statusCode) {
		String name;
		try {
			name = GoogleSignInStatusCodes.getStatusCodeString(statusCode);
		} catch (Throwable t) {
			name = "STATUS";
		}

		Log.e(TAG, logPrefix + ": " + name + "(" + statusCode + ")");
		return RESPONSE_EXCEPTION_PREFIX + name + "(" + statusCode + ")";
	}

	/**
	 * Describe any throwable, including the ones with no message. {@link NoClassDefFoundError}
	 * carries the missing class name as its message, which is exactly the diagnostic needed when a
	 * consuming project has not supplied {@code play-services-auth}.
	 */
	private static String describeThrowable(Throwable t) {
		final String message = t.getLocalizedMessage();
		if (message == null) {
			return RESPONSE_EXCEPTION_PREFIX + t.getClass().getName();
		}

		return RESPONSE_EXCEPTION_PREFIX + t.getClass().getSimpleName() + ": " + message;
	}

	/**
	 * Send a response back to Unity.
	 *
	 * <p>{@code UnitySendMessage} queues the message for the Unity player loop, so the C# callback
	 * runs on the main thread on a later frame regardless of which thread calls this. Note that it
	 * <em>silently drops</em> the message if the player's native libraries are not loaded (it logs
	 * "Native libraries not loaded - dropping message") - which is why the C# side puts a timeout on
	 * every silent request rather than trusting that a response always arrives.
	 */
	static void sendResponse(String unityObject, String unityMethod, String message) {
		try {
			UnityPlayer.UnitySendMessage(unityObject, unityMethod, message);
		} catch (Throwable t) {
			// Nothing useful is left to do: the channel we would report the failure over is the one
			// that just failed.
			Log.e(TAG, "Could not send response to Unity", t);
		}
	}
}
