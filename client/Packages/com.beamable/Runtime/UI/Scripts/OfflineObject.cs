using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Beamable;
using Beamable.UI.Scripts;

public class OfflineObject : MonoBehaviour
{
    private Beamable.IBeamableAPI _engineInstance;

    public UIBehaviour Component;

    private async void Start()
    {
        _engineInstance = await Beamable.API.Instance;
        _engineInstance.ConnectivityService.OnConnectivityChanged += toggleOfflineMode;
        ObtainSupportedComponent();
        if (!_engineInstance.ConnectivityService.HasConnectivity)
        {
            toggleOfflineMode(false);
        }
    }

    private void toggleOfflineMode(bool offlineStatus)
    {
        if (Component != null)
        {
            switch (Component)
            {
                case Button b:
                    b.interactable = offlineStatus;
                    break;
                case TMP_InputField t:
                    t.interactable = offlineStatus;
                    break;
                default:
                    Debug.LogWarning("No Offline Functionality selected for GameObject: " + gameObject.name +
                        ". Consider removing this component if not planned for use.");
                    break;
            }
        }
    }

    private void ObtainSupportedComponent()
    {
        switch(Component)
        {
            case Button b:
                Component = gameObject.GetComponent<Button>();
                break;
            case TMP_InputField t:
                Component = gameObject.GetComponent <TMP_InputField>();
                break;
        }
    }

    public void OnDestroy()
    {
        if(_engineInstance != null)
        {
            _engineInstance.ConnectivityService.OnConnectivityChanged -= toggleOfflineMode;
        }
        Destroy(this);
    }
}


