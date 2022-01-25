using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Config;
using Beamable.Coroutines;
using System.Linq;
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
        bool HasConnectivity { get; }
        bool ForceDisabled
        {
	        get;
	        set;
        }

        bool Disabled
        {
	        get;
        }

        event Action<bool> OnConnectivityChanged;
        Promise SetHasInternet(bool hasInternet);
        Promise ReportInternetLoss();
        void OnReconnectOnce(Action onReconnection);
        void OnReconnectOnce(ConnectionCallback promise, int order = 0);
    }

    public delegate Promise ConnectionCallback();

    public static class IConnectivityServiceExtensions
    {
	    private static bool _globalForceDisabled;
	    public static bool GlobalForceDisabled
	    {
		    get => _globalForceDisabled;
		    set => _globalForceDisabled = value;
	    }

	    public static void SetGlobalEnabled(this IConnectivityService _, bool forceDisabled) =>
		    GlobalForceDisabled = forceDisabled;

	    public static bool GetGlobalEnabled(this IConnectivityService _) => _globalForceDisabled;
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

        public bool HasConnectivity { get; private set; } = true;

        private bool _forceDisabled;
        public bool ForceDisabled
        {
	        get => _forceDisabled;
	        set => _forceDisabled = value;
	        // SetHasInternet(HasConnectivity);
        }

        public bool Disabled => _forceDisabled || IConnectivityServiceExtensions.GlobalForceDisabled;

        public string ConnectivityRoute { get; private set; }
        private bool _first = true;

        public event Action<bool> OnConnectivityChanged;

        private event Action OnReconnection;

        public ConnectivityService( CoroutineService coroutineService)
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
	        yield return new PromiseYieldInstruction(SetHasInternet(true));
            while (true)
            {
                yield return _delay; // don't spam the internet checking...
                _pingStartDateTime = Time.time;

                _request = BuildWebRequest();
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
	                yield return new PromiseYieldInstruction(SetHasInternet(false));
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
	                yield return new PromiseYieldInstruction(SetHasInternet(false));
                }
                else
                {
	                yield return new PromiseYieldInstruction(SetHasInternet(true));
                }
            }
        }

        public async Promise SetHasInternet(bool hasInternet)
        {
	        if (Disabled)
	        {
		        hasInternet = false;
	        }

	        var isReconnection = (hasInternet && !HasConnectivity);
	        var isChange = hasInternet != HasConnectivity;


            HasConnectivity = hasInternet;
            if (isReconnection)
            {
	            _reconnectionPromises.Sort((a, b) => a.Item2.CompareTo(b.Item2));
	            foreach (var reconnection in _reconnectionPromises.ToList())
	            {
		            await reconnection.Item1();
		            _reconnectionPromises.Remove(reconnection);
	            }

	            // we have the tubes! Invoke any pending actions and reset it.
	            OnReconnection?.Invoke();
	            OnReconnection = null;
            }

            if (isChange || _first)
            {
	            _first = false;
	            OnConnectivityChanged?.Invoke(hasInternet);
            }
        }

        public Promise ReportInternetLoss()
        {
            // TODO: This could expand into a loss-tolerance system where the connectivity service could allow a few failed messages before declaring internet is entirely gone.
            // but for now, just report no internet...
            return SetHasInternet(false);
        }

        private List<(ConnectionCallback, int)> _reconnectionPromises = new List<(ConnectionCallback, int)>();
        public void OnReconnectOnce(ConnectionCallback callback, int order = 0)
        {
	        if (HasConnectivity)
	        {
		        var _ = callback();
		        return;
	        }

	        _reconnectionPromises.Add((callback, order));

        }


        public void OnReconnectOnce(Action onReconnection)
        {
	        // if the state is already in a connected sense, then run this immediately.
	        if (HasConnectivity)
	        {
		        onReconnection?.Invoke();
		        return;
	        }

	        // queue the action to be run when the connectivity comes back.
	        OnReconnection += onReconnection;
        }

        public class PromiseYieldInstruction : CustomYieldInstruction
        {
	        private readonly PromiseBase _promise;

	        public PromiseYieldInstruction(PromiseBase promise)
	        {
		        _promise = promise;
	        }

	        public override bool keepWaiting => !_promise.IsCompleted;
        }
    }
}
