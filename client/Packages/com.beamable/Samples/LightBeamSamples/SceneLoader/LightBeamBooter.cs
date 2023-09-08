using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightBeamBooter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
	    var index = GetSceneIndex();
	    SceneManager.LoadSceneAsync(index);
    }

    static int GetSceneIndex()
    {
	    var url = Application.absoluteURL;
	    var queryStartIndex = url.IndexOf("?", StringComparison.Ordinal);
	    if (queryStartIndex == -1)
	    {
		    Debug.LogWarning("no query args");
		    return 1;
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

		    return 1;
	    } 
	    
	    // pretend the sceneName is an int, for now.
	    if (int.TryParse(sceneName, out var sceneIndex))
	    {
		    Debug.Log($"found scene name=[{sceneName}] index=[{sceneIndex}]");
		    return sceneIndex;
	    }

	    Debug.LogWarning("no parsable scene arg");
	    return 1;

    }

}
