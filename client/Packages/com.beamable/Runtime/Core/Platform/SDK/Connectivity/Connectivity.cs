using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Config;
using Beamable.Coroutines;
using UnityEngine;
using UnityEngine.Networking;

namespace Beamable.Api.Connectivity
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Connectivity feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/connectivity-feature">Connectivity</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public interface IConnectivityService
	{
		bool HasConnectivity
		{
			get;
		}

		event Action<bool> OnConnectivityChanged;
		void SetHasInternet(bool hasInternet);
		void ReportInternetLoss();
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %Connectivity feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/connectivity-feature">Connectivity</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class ConnectivityService : IConnectivityService
	{
		private const float _secondsBeforeTimeout = 5.0f;
		private const float _secondsBetweenCheck = 3;

		private const string PLATFORM_CONFIG_KEY = "platform";
		private const string HEALTH_ROUTE_CONFIG_KEY = "connectivityRoute";
		private const string DEFAULT_HEALTH_PATH = "/health";

		private float _pingStartDateTime;
		private UnityWebRequest _request;
		private WaitForSeconds _delay;
		private readonly string _host;
		private CoroutineService _coroutineService;

		public bool HasConnectivity
		{
			get;
			private set;
		} = true;

		public string ConnectivityRoute
		{
			get;
			private set;
		}

		private bool _first = true;

		public event Action<bool> OnConnectivityChanged;

		public ConnectivityService(CoroutineService coroutineService)
		{
			_delay = new WaitForSeconds(_secondsBetweenCheck);
			_host = ConfigDatabase.GetString(PLATFORM_CONFIG_KEY);
			_coroutineService = coroutineService;

			if (!ConfigDatabase.TryGetString(HEALTH_ROUTE_CONFIG_KEY, out var route))
			{
				route = DEFAULT_HEALTH_PATH;
			}

			ConnectivityRoute = route.Contains("://") ? route : $"{_host}{route}";
			_coroutineService.StartCoroutine(MonitorConnectivity());
		}

		private UnityWebRequest BuildWebRequest()
		{
			// Prepare the request
			var request = new UnityWebRequest(ConnectivityRoute)
			{
				downloadHandler = new DownloadHandlerBuffer(), method = Method.GET.ToString()
			};
			return request;
		}

		private IEnumerator MonitorConnectivity()
		{
			SetHasInternet(true);
			while (true)
			{
				yield return _delay; // don't spam the internet checking...
				_pingStartDateTime = Time.time;

				_request = BuildWebRequest();
				if (Application.internetReachability == NetworkReachability.NotReachable)
				{
					SetHasInternet(false);
					continue;
				}

				_request.SendWebRequest();

				var isTimeout = false;
				while (!_request.isDone && !isTimeout)
				{
					isTimeout = Time.time - _pingStartDateTime > _secondsBeforeTimeout;
					yield return null;
				}

				if (isTimeout || _request.IsNetworkError())
				{
					SetHasInternet(false);
				}
				else
				{
					SetHasInternet(true);
				}
			}
		}

		public void SetHasInternet(bool hasInternet)
		{
			if (hasInternet != HasConnectivity || _first)
			{
				_first = false;
				OnConnectivityChanged?.Invoke(hasInternet);
			}

			HasConnectivity = hasInternet;
		}

		public void ReportInternetLoss()
		{
			// TODO: This could expand into a loss-tolerance system where the connectivity service could allow a few failed messages before declaring internet is entirely gone.
			// but for now, just report no internet...
			SetHasInternet(false);
		}
	}
}
