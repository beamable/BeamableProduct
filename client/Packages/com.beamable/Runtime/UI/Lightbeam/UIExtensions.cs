using Beamable.Common;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Beamable.Runtime.LightBeams
{
	public static class LightBeamUtilExtensions
	{
		public static Dictionary<string, string> Hints = new Dictionary<string, string>();
		
		/// <summary>
		/// Remove all child gameObjects from the given transform
		/// </summary>
		public static void Clear(this Transform transform)
		{
			for (var i = 0; i < transform.childCount; i++)
			{
				Object.Destroy(transform.GetChild(i).gameObject);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_"></param>
		/// <param name="enabled"></param>
		/// <param name="objects"></param>
		public static void EnableObjects(this ILightRoot _, bool enabled, params Component[] objects)
		{
			foreach (var component in objects)
			{
				component.gameObject.SetActive(enabled);
			}
		}
		
		public static void EnableObjects(this ILightRoot root, params Component[] objects)
		{
			root.EnableObjects(true, objects);
		}
		
		public static void DisableObjects(this ILightRoot root, params Component[] objects)
		{
			root.EnableObjects(false, objects);
		}

		public static void HandleClicked(this Button button, Action onClick)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => onClick());
		}

		public static void HandleClicked(this Button button, Func<Promise> onClick) =>
			HandleClicked(button, null, onClick);
		public static void HandleClicked(this Button button, string text, Func<Promise> onClick)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(async () =>
			{
				var action = onClick();
				await button.ShowLoading(text, action);
			});
		}

		public static Promise<T> ShowLoading<T>(this Promise<T> promise, LightBeam beam)
		{
			return ShowLoading(promise.ToPromise(), beam)
				.Map(_ => promise.GetResult())
				.Error(ex => throw ex);
		}
		public static Promise ShowLoading<T>(this T promise, LightBeam beam)
			where T : Promise<Unit>
		{
			return ShowLoading(promise.ToPromise(), beam);
		}

		public static async Promise LoadingFadeIn(this LightBeam beam)
		{
			var service = beam.Scope.GetService<CoroutineService>();
			service.StopAll("loading");
			var promise = new Promise();
			service.StartNew("loading", Animate());
			IEnumerator Animate()
			{
				beam.LoadingBlocker.gameObject.SetActive(true);
				var fadeIn = Lerp(x => beam.LoadingBlocker.alpha = x, beam.LoadingBlocker.alpha, 1);
				foreach (var p in fadeIn)
				{
					yield return p;
				}
				promise.CompleteSuccess();
			}

			await promise;
		}
		
		public static async Promise ShowLoading(this Promise promise, LightBeam beam)
		{
			if (beam.LoadingBlocker == null)
			{
				await promise;
				return;
			}
			
			
			var service = beam.Scope.GetService<CoroutineService>();
			
			service.StopAll("loading");
			service.StartNew("loading", Animate());
			IEnumerator Animate()
			{
				Func<bool> isWorking = () => !promise.IsCompleted && !promise.IsFailed;
				yield return null;
				beam.LoadingBlocker.gameObject.SetActive(true);
				var fadeIn = Lerp(x => beam.LoadingBlocker.alpha = x, beam.LoadingBlocker.alpha, 1);
				foreach (var p in fadeIn)
				{
					if (!isWorking()) break;
					yield return p;
				}
				
				while (isWorking())
				{
					yield return null;
				}
				
				var fadeOut = Lerp(x => beam.LoadingBlocker.alpha = x, 1, 0);
				foreach (var p in fadeOut)
				{
					yield return p;
				}

				beam.LoadingBlocker.gameObject.SetActive(false);
			}
			
			await promise;
		}
		
		static IEnumerable Lerp( Action<float> valueCallback, float start, float end, float delay=0,float duration=.1f, Action cb=null)
		{
			yield return new WaitForSecondsRealtime(delay);
			var startTime = Time.realtimeSinceStartup;
			var endTime = startTime + duration;
			valueCallback(start);
			var resolution = new WaitForSecondsRealtime(.01f);
			while ( Time.realtimeSinceStartup <= endTime)
			{
				var r = 1 - (endTime - Time.realtimeSinceStartup) / (endTime - startTime);
				var value = Mathf.Lerp(start, end, r);
				valueCallback(value);
				yield return resolution;
			}
			valueCallback(end);
			cb?.Invoke();
		}

		private static async Promise ShowLoading(this Button button, string loadingText, Promise promise)
		{
			button.interactable = false;
			var text = button.GetComponentInChildren<TMP_Text>();
			var originalText = text.text;
			text.text = loadingText ?? "loading...";
			await promise;

			if (text)
			{
				text.text = originalText;
			}

			if (button)
			{
				button.interactable = true;
			}
		}
	}
	
}
