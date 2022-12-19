using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Blazored.LocalStorage;
using Newtonsoft.Json;

namespace beamWasm.BeamBlazor;

public interface IAppContext
{
	public string Cid { get; }
	public string Pid { get; }
	public string Host { get; }
	public IAccessToken Token { get; }
	public string RefreshToken { get; }
	Task Init();

	Task UpdateToken(TokenResponse response);
}

public class BlazorAppContext : IAppContext
{
	private readonly ILocalStorageService _localStorageService;
	public string Cid { get; private set; }
	public string Pid { get; private set; }
	public string Host { get; private set; }
	public IAccessToken Token => _token;
	public string RefreshToken => _token?.RefreshToken;

	private AccessToken _token;
	private bool _isInitialized = false;

	public BlazorAppContext(ILocalStorageService localStorageService)
	{
		_localStorageService = localStorageService;
	}

	public async Task Init()
	{
		if (_isInitialized)
			return;
		Cid = await GetKeyOr("BlazorCid", "1422202535673860");
		Pid = await GetKeyOr("BlazorPid","DE_1422202535673861");
		Host = await GetKeyOr("BlazorHost", "https://dev.api.beamable.com");
		string tokenJson = await GetKeyOr("BlazorToken", string.Empty);
		if (!string.IsNullOrWhiteSpace(tokenJson))
		{
			_token = JsonConvert.DeserializeObject<AccessToken>(tokenJson);
		}
		_isInitialized = true;
	}

	public async Task UpdateToken(TokenResponse response)
	{
		_token = new AccessToken(response, Cid, Pid);
		await Save("BlazorToken", _token);
	}

	private async Task SetKey(string key, string value)
	{
		await _localStorageService.SetItemAsStringAsync(key, value);
	}

	private async Task Save<T>(string key, T value)
	{
		var json = JsonConvert.SerializeObject(value);
		await _localStorageService.SetItemAsStringAsync(key, json);
	}

	private async Task<string> GetKeyOr(string key, string defaultValue)
	{
		var result = await _localStorageService.GetItemAsStringAsync(key);

		return string.IsNullOrWhiteSpace(result) ? defaultValue : result;
	}
}
