using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DragDropSceneSample : MonoBehaviour
{
    public void OpenDragDropScene()
    {
        var asd = SceneManager.LoadSceneAsync("TestScene - 3D Scene", LoadSceneMode.Additive);
        asd.completed += operation => Debug.Log("Loaded the Scene!!!!");
    }
}
