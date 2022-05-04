using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ConsoleDisplay : MonoBehaviour
{
	private TextMeshProUGUI text;
	readonly Queue<string> logs = new Queue<string>(size);
	readonly StringBuilder builder = new StringBuilder();
	private const int size = 50;

	void OnDisable()
	{
		Application.logMessageReceived -= handleLog;
	}
        
	void Awake()
	{
		text = GetComponent<TextMeshProUGUI>();
		text.text = string.Empty;
		Debug.Log("Test one");
		Debug.LogAssertion("LOG ASSERT");
	}

	private void OnEnable()
	{
		Application.logMessageReceived += handleLog;
		updateText();
		Debug.LogError("ONEONEENE11!");
	}

	private void handleLog(string condition, string stacktrace, LogType type)
	{
		logs.Enqueue($"{type.ToString()}: {condition}");
		if (logs.Count > size)
		{
			logs.Dequeue();
		}
		if(gameObject.activeInHierarchy)
			updateText();
	}

	private void updateText()
	{
		builder.Clear();
		foreach (var log in logs)
		{
			builder.AppendLine(log);
		}

		text.text = builder.ToString();
	}
}

