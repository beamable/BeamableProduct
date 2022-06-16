using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Serilog;

namespace Beamable.Server;

public class SocketDaemon
{
	private readonly IMicroserviceArgs _env;
	private readonly MicroserviceRequester _requester;
	private readonly SocketRequesterContext _socketContext;

	private SocketDaemon(IMicroserviceArgs env, MicroserviceRequester requester, SocketRequesterContext socketContext)
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
				if (_socketContext.AuthorizationCounter > 0)
				{
					// TODO do the authorization.
					await Authenticate();

					Interlocked.Exchange(ref _socketContext.AuthorizationCounter, 0);
				}
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError("Authorization failed.");
				BeamableLogger.LogException(ex);
			}

			await Task.Yield();
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

	/// <summary>
	/// Kick off a long running task that will make sure the given <see cref="socketContext"/> is authenticated.
	/// The daemon is running in a loop, checking the <see cref="SocketRequesterContext.AuthorizationCounter"/> field.
	/// When it is positive, the daemon will start ONE authorization flow, and then set the value to zero.
	/// </summary>
	/// <param name="env"></param>
	/// <param name="requester"></param>
	/// <param name="socketContext"></param>
	/// <param name="cancellationTokenSource"></param>
	/// <returns>A task that completes the loop after the given <see cref="cancellationTokenSource"/> has requested a cancel</returns>
	public static Task Start(
		IMicroserviceArgs env, MicroserviceRequester requester, SocketRequesterContext socketContext,
		CancellationTokenSource cancellationTokenSource)
	{
		var daemon = new SocketDaemon(env, requester, socketContext);
		return Task.Run(() => daemon.Run(cancellationTokenSource), cancellationTokenSource.Token);
	}
}