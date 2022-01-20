using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Assistant;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Server.Clients;
using System.Linq;
using UnityEngine;

public class TestCacheDependentMS : MonoBehaviour
{
	private string globallyText;
	private string beamContextText;
	private List<int> _usefulListOfInts = new List<int>();
	private TestGloballyAccessibleHintSystem _userHintSystem = null;

	private void OnGUI()
	{
		if (GUILayout.Button("Get Cached Data in MS"))
		{
			// var ms = new CacheDependentMSClient();
			//
			// var asd = await ms.GetCachedView();
			// Debug.Log(asd.ToString());    
		}

		GUILayout.Label(globallyText);
		GUILayout.Label(beamContextText);
	}

	private void Awake()
	{
		// Calling this just to see if we don't have a static initialization problem...
		//Beam.ReflectionCache.ClearProviders();
		
		var testBeamContextSystem = BeamContext.Default.ServiceProvider.GetService<TestBeamContextSystem>();
		testBeamContextSystem.DoSomething(ref beamContextText);

		for (int i = 0; i < 2; i++)
		{
			_usefulListOfInts.Add(UnityEngine.Random.Range(0, 100));
		}

#if UNITY_EDITOR
		Beamable.BeamEditor.GetBeamHintSystem(ref _userHintSystem);
#endif
		_userHintSystem.AddHintTest(_usefulListOfInts);

		globallyText = string.Join(",", _usefulListOfInts);
	}
}

[BeamHintSystem(true)]
public class TestBeamContextHintSystem : IBeamHintSystem
{
	private IBeamHintPreferencesManager _preferencesManager;
	private IBeamHintGlobalStorage _globalStorage;

	public TestBeamContextHintSystem() { }

	public void SetPreferencesManager(IBeamHintPreferencesManager preferencesManager)
	{
		_preferencesManager = preferencesManager;
	}

	public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
	{
		_globalStorage = hintGlobalStorage;
	}

	public void OnInitialized()
	{
		BeamableLogger.Log("TestBeamHintContextHintSystem Initialized!!!");
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public void AddHintTest(long playerId, string param2)
	{
		var hintId = BeamHintIds.GenerateHintId("Test Hint Id");
		var parameterizedHintId = BeamHintIds.AppendHintIdParams(hintId, "_", playerId);
		_globalStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.GenerateUserDomain("TestDomain"), parameterizedHintId, param2);
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public void TestFillOutEditorOnlyDataWithText(ref string text)
	{
		text = "TEST EDITOR ONLY STRING ---- YOU SHOULD NOT SEE THIS IN THE BUILD";
	}
}

public class TestBeamContextSystem
{
	private IUserContext _context;
	private TestBeamContextHintSystem _hintSystem;

#if UNITY_EDITOR
	public TestBeamContextSystem(TestBeamContextHintSystem hintSystem, IUserContext ctx) : this(ctx)
	{
		_hintSystem = hintSystem;
	}
#endif

	public TestBeamContextSystem(IUserContext ctx)
	{
		_context = ctx;
	}

	public void DoSomething(ref string textToFill)
	{
		Debug.Log("Doing Something system-y!");
		_hintSystem.AddHintTest(_context.UserId, "TEST CONTEXT OBJECT!!!!");

		textToFill = "";
		_hintSystem.TestFillOutEditorOnlyDataWithText(ref textToFill);
		if (string.IsNullOrEmpty(textToFill))
		{
			textToFill = "THIS WASN'T FILLED BY THE HINT SYSTEM!!!";
		}
	}

	[RegisterBeamableDependencies]
	public static void RegisterDependency(IDependencyBuilder builder)
	{
		builder.AddSingleton<TestBeamContextSystem>();
	}
}

public class TestGloballyAccessibleHintSystem : IBeamHintSystem
{
	private IBeamHintPreferencesManager _preferencesManager;
	private IBeamHintGlobalStorage _globalStorage;

	public TestGloballyAccessibleHintSystem() { }

	public void SetPreferencesManager(IBeamHintPreferencesManager preferencesManager)
	{
		_preferencesManager = preferencesManager;
	}

	public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
	{
		_globalStorage = hintGlobalStorage;
	}

	public void OnInitialized()
	{
		BeamableLogger.Log("TestGloballyAccessibleHintSystem Initialized!!!");
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public void AddHintTest(List<int> dataToCheckForHints)
	{
		var gt50HintId = BeamHintIds.GenerateHintId("Hint Only if an element GT 50");
		if (dataToCheckForHints.Count(i => i > 50) > 0)
			_globalStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.GenerateUserDomain("TestDomain"), gt50HintId, dataToCheckForHints.Where(i => i > 50));

		var le50HintId = BeamHintIds.GenerateHintId("Hint Only if an element LE 50");
		if (dataToCheckForHints.Count(i => i <= 50) > 0)
			_globalStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.GenerateUserDomain("TestDomain"), le50HintId, dataToCheckForHints.Where(i => i <= 50));
	}
}
