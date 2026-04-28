using Beamable;
using UnityEngine;

public class SampleAdminConsole : MonoBehaviour
{
	private BeamContext  _context;
	//private bool _consoleEnabled = false;
	private async void Start()
    {
	    _context = BeamContext.Default;
	    await _context.OnReady;
    }
	
	/*
	private void Update()
	{
		
		if (Input.GetKeyDown(KeyCode.Space))
		{
			_consoleEnabled  = !_consoleEnabled;
			if (_consoleEnabled)
			{
				_context.StartAdminConsole();
			}
			else
			{
				_context.CloseAdminConsole();
			}
		}
	}*/
}
