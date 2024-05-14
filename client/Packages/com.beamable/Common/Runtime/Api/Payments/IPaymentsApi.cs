// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

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
