// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

﻿namespace Beamable.Common.Api.Payments
{
	public interface IPaymentsApi
	{
		/// <summary>
		/// Verify receipt of a given provider.
		/// </summary>
		/// <param name="provider">Accepted values are: "facebook", "googleplay", "itunes", "steam", "windows" and "test".</param>
		/// <param name="receipt">Receipt data.</param>
		Promise<EmptyResponse> VerifyReceipt(string provider, string receipt);
	}
}
