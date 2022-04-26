using Beamable;
using Beamable.Editor;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class BeamableLoginDev
{
	private const string PATH_TO_LOGIN_DATA = "Assets/Editor/BeamableLoginDev/login_data.json";

	[MenuItem("Beamable/Login with dev credentials", false, priority = 0)]
	public static async void Login()
	{
		var editorAPI = BeamEditorContext.Default; 
		await editorAPI.InitializePromise;
		
		var isLoggedIn = editorAPI.CurrentUser != null;

		if (isLoggedIn)
		{
			Debug.LogWarning($"You are already logged in!");
			return;
		}

		var json = File.ReadAllText(PATH_TO_LOGIN_DATA);
		var data = JsonUtility.FromJson<BeamableDevLoginData>(json);

		if (data.IsDataMissing)
		{
			Debug.LogError($"Login data is missing! Check {PATH_TO_LOGIN_DATA}");
			return;
		}

		var password = Encoding.UTF8.GetString(Convert.FromBase64String(data.password));
		await editorAPI.LoginCustomer(data.aliasOrCid, data.email, password);
		await BeamEditorContext.Default.LoginCustomer(data.aliasOrCid, data.email, password);
	}
}

[Serializable]
public class BeamableDevLoginData
{
	public bool IsDataMissing =>
		string.IsNullOrWhiteSpace(aliasOrCid) ||
		string.IsNullOrWhiteSpace(email) ||
		string.IsNullOrWhiteSpace(password);

	public string aliasOrCid;
	public string email;
	public string password;
}
