//#define PREPEND_SPEW_METADATA
// With PREPEND_SPEW_METADATA defined, all spew logs will have frame # and time prepended.

using System;
using System.Diagnostics;

#if PREPEND_SPEW_METADATA
using UnityEngine;
#endif

namespace Beamable.Spew
{
	public class Logger
	{
#if UNITY_EDITOR
		static Logger()
		{
			filter = UnityEditor.EditorPrefs.GetString("SpewFilter", "");
		}
#endif

#if PREPEND_SPEW_METADATA
      private static int _lastFrame;
      private static string _lastColor = "orange";

      static string PrependMetadata(string msg)
      {
         if (_lastFrame != Time.frameCount)
         {
            _lastFrame = Time.frameCount;
            _lastColor = _lastColor == "orange" ? "lightblue" : "orange";
         }

         return $"[<color={_lastColor}>{Time.frameCount}</color>:{Time.realtimeSinceStartup}] {msg}";
      }
#endif

		public static string filter = "";

		public static void DoSpew(string msg, params object[] args)
		{
			var s = string.Format(msg, args);
			if (!string.IsNullOrEmpty(filter) && !s.Contains(filter))
				return;

#if PREPEND_SPEW_METADATA
         s = PrependMetadata(s);
#endif

			UnityEngine.Debug.Log(s);
		}

		public static void DoSpew(object msg)
		{
			var s = msg.ToString();
			if (!string.IsNullOrEmpty(filter) && !s.Contains(filter))
				return;

#if PREPEND_SPEW_METADATA
         s = PrependMetadata(s);
#endif

			UnityEngine.Debug.Log(s);
		}

		public static void DoSpew(object msg, UnityEngine.Object context)
		{
			var s = msg.ToString();
			if (!string.IsNullOrEmpty(filter) && !s.Contains(filter))
				return;

#if PREPEND_SPEW_METADATA
         s = PrependMetadata(s);
#endif

			UnityEngine.Debug.Log(s, context);
		}
	}

	[SpewLogger]
	public static class NotificationLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_NOTIFICATION)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_NOTIFICATION)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class ChatLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_CHAT)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_CHAT)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class PubnubSubscriptionLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_PUBNUB_SUBSCRIPTION)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL)] [Conditional(SpewConstants.SPEW_PUBNUB_SUBSCRIPTION)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class TutorialLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_TUTORIAL)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_TUTORIAL)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class UIMaterialFillLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_UI_MATERIAL_FILL)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL)] [Conditional(SpewConstants.SPEW_UI_MATERIAL_FILL)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class TouchLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_TOUCH)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL)] [Conditional(SpewConstants.SPEW_TOUCH)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class GraphicsLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_GRAPHICS)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_GRAPHICS)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class AssetBundleLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_ASSETBUNDLE)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_ASSETBUNDLE)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class BuildLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_BUILD)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}
	}

	[SpewLogger]
	public static class ServerStateLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SERVERSTATE)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SERVERSTATE)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class InAppPurchaseLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_IAP)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_IAP)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger] public static class MailLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_MAIL)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}
	}

	[SpewLogger]
	public static class CloudOnceLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_CLOUDONCE)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_CLOUDONCE)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class ApptentiveLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_APPTENTIVE)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_APPTENTIVE)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class AnalyticsLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_ANALYTICS)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_ANALYTICS)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class NetMsgLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_NETMSG)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_NETMSG)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class PlatformLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_PLATFORM)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_PLATFORM)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class ResourcesLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_RESOURCES)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_RESOURCES)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class AppLifetimeLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_APP_LIFETIME)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_APP_LIFETIME)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class SVGLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SVG)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SVG)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}

	[SpewLogger]
	public static class ServicesLogger
	{
		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SERVICES)]
		public static void Log(object msg)
		{
			Logger.DoSpew(msg);
		}

		[Conditional(SpewConstants.SPEW_ALL), Conditional(SpewConstants.SPEW_SERVICES)]
		public static void LogFormat(string msg, params object[] args)
		{
			Logger.DoSpew(msg, args);
		}
	}
}
