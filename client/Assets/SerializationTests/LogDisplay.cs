using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LogDisplay : MonoBehaviour
{
	private Text text;

	Queue _queue = new Queue();
    // Start is called before the first frame update
    void Awake()
    {
	    text = GetComponent<Text>();
        Application.logMessageReceived += ApplicationOnlogMessageReceived;
    }

    private void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
    {
	    _queue.Enqueue(condition);
	    while (_queue.Count > 25)
	    {
		    _queue.Dequeue();
	    }
	    text.text = string.Join("\n", _queue.ToArray());
    }

    // Update is called once per frame
    void OnDestroy()
    {
        
    }
}
