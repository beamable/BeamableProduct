using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightBeamBooter : MonoBehaviour
{
	[Header("Asset References")]
	public LightBeamSceneConfigObject config;
	
    // Start is called before the first frame update
    void Start()
    {
	    var index = GetSceneName(config);
	    SceneManager.LoadSceneAsync(index);
    }

    static string GetSceneName(LightBeamSceneConfigObject config)
    {
	    var url = Application.absoluteURL;
	    var queryStartIndex = url.IndexOf("?", StringComparison.Ordinal);
	    if (queryStartIndex == -1)
	    {
		    Debug.LogWarning("no query args");
		    return null;
	    }

	    var queryStr = url.Substring(queryStartIndex + 1); // consume the ?
	    var queryArgStrs = queryStr.Split('&', StringSplitOptions.RemoveEmptyEntries);

	    var queryArgs = new Dictionary<string, string>();
	    
	    foreach (var query in queryArgStrs)
	    {
		    var parts = query.Split('=', StringSplitOptions.RemoveEmptyEntries);
		    switch (parts.Length)
		    {
			    case 1:
				    queryArgs[parts[0]] = "";
				    break;
			    case 2:
				    queryArgs[parts[0]] = parts[1];
				    break;
			    default:
				    break;
		    }
	    }

	    if (!queryArgs.TryGetValue("scene", out var sceneName))
	    {
		    Debug.LogWarning("no scene arg");
		    return null;
	    }

	    if (config.TryGetScene(sceneName, out var scene))
	    {
		    return scene.sceneName;
	    }
	    
	    Debug.LogWarning("no parsable scene arg");
	    return null;

    }

}
