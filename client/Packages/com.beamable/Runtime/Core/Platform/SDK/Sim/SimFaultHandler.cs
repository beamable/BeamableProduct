using Beamable.Api;
using Beamable.Common;
using JetBrains.Annotations;
using System;

namespace Beamable.Experimental.Api.Sim
{
	public interface ISimFaultHandler
	{

	}

	public class SimFaultHandler : ISimFaultHandler
	{
		public void HandleSyncSuccess()
		{

		}

		public Promise<SimFaultResult> HandleSyncError(Exception exception)
		{
			// TODO: handle 502's and 504's for up to 15 seconds...

			switch (exception)
			{
				default:
					return Promise<SimFaultResult>.Failed(exception);
			}
		}
	}

	public struct SimFaultResult
	{
		/// <summary>
		/// True if the fault was handled correctly, false if the error should cause an outage in the service.
		/// </summary>
		public bool Recovered;

		/// <summary>
		/// The error message that should be displayed in the event that <see cref="Recovered"/> is false.
		/// </summary>
		public string ErrorMessage;
	}
}
