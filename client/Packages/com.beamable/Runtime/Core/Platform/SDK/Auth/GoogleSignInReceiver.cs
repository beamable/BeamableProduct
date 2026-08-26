using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

namespace Beamable.Platform.SDK.Auth
{
	/// <summary>
	/// A short-lived, hidden GameObject that exists only to receive one native Google Sign-In
	/// response via <c>UnitySendMessage</c>, then destroys itself.
	/// </summary>
	/// <remarks>
	/// <para>The native plugins answer by name: <c>UnitySendMessage(objectName, methodName, message)</c>.
	/// That carries no correlation id, so <b>the only correlation channel available is the object
	/// name</b> - which is why a receiver is created per request with a unique name instead of
	/// sharing one long-lived object. A shared receiver would have to match responses to requests by
	/// arrival order, which breaks as soon as a silent and an interactive attempt overlap.</para>
	///
	/// <para>Created by <see cref="GoogleSignInService"/>; there is no reason to use this directly.</para>
	/// </remarks>
	[AddComponentMenu("")]
	public class GoogleSignInReceiver : MonoBehaviour
	{
		/// <summary>
		/// The method name the native plugins are told to call. Kept in one place because it travels
		/// to native code as a string and a typo would simply mean the response is silently dropped.
		/// </summary>
		public const string RESPONSE_METHOD = nameof(OnGoogleSignInResponse);

		/// <summary>
		/// A single frame's contribution to the timeout is clamped to this. After the app returns
		/// from the background <see cref="Time.unscaledDeltaTime"/> can report the entire time spent
		/// paused, which would otherwise expire a request the instant the player resumes.
		/// </summary>
		private const float MAX_TIMEOUT_STEP_SECONDS = 0.25f;

		private Action<string> _onResponse;
		private bool _settled;

		/// <summary>
		/// Create a receiver for one request.
		/// </summary>
		/// <param name="objectName">
		/// Unique GameObject name; this is what the native side sends to.
		/// </param>
		/// <param name="onResponse">
		/// Invoked exactly once, on the main thread, with the raw native message - or with an
		/// UNAVAILABLE sentinel if the receiver is torn down or times out first.
		/// </param>
		/// <param name="timeoutSeconds">
		/// Abandon the request after this much running-player time. Pass 0 or less for no timeout.
		/// </param>
		public static GoogleSignInReceiver Create(string objectName, Action<string> onResponse, float timeoutSeconds)
		{
			var gob = new GameObject(objectName);
			DontDestroyOnLoad(gob);

			// DontSave only, matching BeamableGlobalGameObject. Deliberately not HideInHierarchy: it
			// gains nothing at runtime, and its interaction with Unity's internal name lookup for
			// UnitySendMessage is undocumented.
			gob.hideFlags = HideFlags.DontSave;

			var receiver = gob.AddComponent<GoogleSignInReceiver>();
			receiver._onResponse = onResponse;

			if (timeoutSeconds > 0f)
			{
				receiver.StartCoroutine(receiver.AbandonAfter(timeoutSeconds));
			}

			return receiver;
		}

		/// <summary>
		/// The <c>UnitySendMessage</c> entry point. Called by name from the Android plugin and the
		/// iOS plugin, so the name is part of the contract and it must survive managed code
		/// stripping - hence <see cref="PreserveAttribute"/>.
		/// </summary>
		[Preserve]
		private void OnGoogleSignInResponse(string message)
		{
			Settle(message);
		}

		private IEnumerator AbandonAfter(float timeoutSeconds)
		{
			var elapsed = 0f;

			while (elapsed < timeoutSeconds)
			{
				yield return null;

				if (_settled)
				{
					yield break;
				}

				elapsed += Mathf.Min(Time.unscaledDeltaTime, MAX_TIMEOUT_STEP_SECONDS);
			}

			// A response can still arrive after this, addressed to a GameObject that no longer
			// exists; Unity logs "SendMessage: object <name> not found" and moves on. That is why the
			// default timeout is generous - see GoogleSignInService.DEFAULT_SILENT_TIMEOUT_SECONDS.
			Settle($"{GoogleSignInResult.SENTINEL_UNAVAILABLE} - no response from the Google Sign-In " +
				   $"plugin within {timeoutSeconds:0.#}s");
		}

		private void Settle(string message)
		{
			if (_settled)
			{
				return;
			}

			_settled = true;

			var callback = _onResponse;
			_onResponse = null;

			// Destroy first so a throwing callback cannot leak the receiver.
			Destroy(gameObject);

			callback?.Invoke(message);
		}

		/// <summary>
		/// Resolve anything still pending when the receiver goes away - a scene teardown, leaving
		/// play mode, or a domain reload. Without this, awaiting callers would hang forever instead
		/// of unwinding.
		/// </summary>
		private void OnDestroy()
		{
			if (_settled)
			{
				return;
			}

			_settled = true;

			var callback = _onResponse;
			_onResponse = null;

			callback?.Invoke($"{GoogleSignInResult.SENTINEL_UNAVAILABLE} - the Google Sign-In receiver " +
							 "was destroyed before a response arrived");
		}
	}
}
