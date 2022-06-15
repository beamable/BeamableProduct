using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Serilog;

namespace Beamable.Server;

public class SocketDaemen
{
	private readonly IMicroserviceArgs _env;
	private readonly MicroserviceRequester _requester;
	private readonly SocketRequesterContext _socketContext;

	private SocketDaemen(IMicroserviceArgs env, MicroserviceRequester requester, SocketRequesterContext socketContext)
	{
		_env = env;
		_requester = requester;
		_socketContext = socketContext;
	}

	private async Task Run(CancellationTokenSource cancellationTokenSource)
	{
		while (!cancellationTokenSource.IsCancellationRequested)
		{
			try
			{
				if (_socketContext.AuthorizationRequested)
				{
					// TODO do the authorization.
					await Authenticate();

					_socketContext.AuthorizationRequested = false;
				}
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError("Authorization failed.");
				BeamableLogger.LogException(ex);
			}

			await Task.Delay(1, cancellationTokenSource.Token);
		}
	}

	private async Task Authenticate()
	{
		string CalculateSignature(string text)
		{
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] data = Encoding.UTF8.GetBytes(text);
			byte[] hash = md5.ComputeHash(data);
			return Convert.ToBase64String(hash);
		}

		Log.Debug("Authorizing WS connection");
		var res = await _requester.Request<MicroserviceNonceResponse>(Method.GET, "gateway/nonce");
		Log.Debug("Got nonce");
		var sig = CalculateSignature(_env.Secret + res.nonce);
		var req = new MicroserviceAuthRequest
		{
			cid = _env.CustomerID,
			pid = _env.ProjectName,
			signature = sig
		};
		var authRes = await _requester.Request<MicroserviceAuthResponse>(Method.POST, "gateway/auth", req);
		if (!string.Equals("ok", authRes.result))
		{
			Log.Error("Authorization failed. result=[{result}]", authRes.result);
			throw new Exception("Authorization failed");
		}

		Log.Debug("Authorization complete");
	}

	public static Task Start(
		IMicroserviceArgs env, MicroserviceRequester requester, SocketRequesterContext socketContext,
		CancellationTokenSource cancellationTokenSource)
	{
		var daemon = new SocketDaemen(env, requester, socketContext);
		return daemon.Run(cancellationTokenSource);
	}
}