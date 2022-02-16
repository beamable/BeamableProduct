using Beamable;
using Beamable.Common.Content;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour
{
	public int numberOfObjects = 1100;
	public TextMeshProUGUI logText;

	private void Start()
	{
		logText.text = string.Empty;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit(0);
		}
	}

	public async void Init()
	{
		long sum = 0;
		Log("Initializing Beamable API...");
		Stopwatch watch = Stopwatch.StartNew();
		var api = await API.Instance;
		long elapsed = watch.ElapsedMilliseconds;
		Log($"Beamable API initialized in {elapsed}ms");
		sum += elapsed;
		watch = Stopwatch.StartNew();
		var manifest = await api.ContentService.GetManifest();
		elapsed = watch.ElapsedMilliseconds;
		Log($"Content manifest received in {elapsed}ms");
		sum += elapsed;
		watch = Stopwatch.StartNew();
		// var content = await manifest.ResolveAll();
		// var content = await manifest.entries[0].Resolve();
		var content = await manifest.entries.GetRange(0, Mathf.Min(numberOfObjects, manifest.entries.Count)).ResolveAll();
		elapsed = watch.ElapsedMilliseconds;
		Log($"Full content ({content.Count} items) resolved in {elapsed}ms");
		sum += elapsed;
		watch.Stop();
		Log($"Sum: {sum}ms");
	}

	private void Log(string log)
	{
		if (string.IsNullOrEmpty(logText.text))
		{
			logText.text = log;
		}
		else
		{
			logText.text += "\n" + log;
		}

		Debug.LogError($"[TEST] {log}");
	}
}
