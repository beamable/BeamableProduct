using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Blazored.LocalStorage;

namespace blazor_portal.Services;

public interface IContext
{
	Task Set(string cid, string pid, string host);
	Task UpdateToken(TokenResponse token);
	IAccessToken Token { get; }
	string Cid { get; }
	string Pid { get; }
	string Host { get; }
}

public class BlazorContext : IContext
{
	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;
	public IAccessToken Token => _token;
	private BlazorToken _token = new BlazorToken();
	private string _cid, _pid;
	private string _host = "https://api.beamable.com";
	private readonly ILocalStorageService _localStorage;

	public BlazorContext(ILocalStorageService localStorage)
	{
		_localStorage = localStorage;
	}

	public async Task<string[]> GetSavedTokenList()
	{
		var token = await _localStorage.KeysAsync();
		return token.Where(s => s.StartsWith("token.")).ToArray();
	}

	public async Task UpdateToken(TokenResponse token)
	{
		_token = new BlazorToken(token, _cid, _pid);
		await _localStorage.SetItemAsync($"token.{_cid}", _token);
	}
	
	public async Task Set(string cid, string pid, string host)
	{
		_cid = cid;
		_pid = pid;
		_host = host;
		_token = new BlazorToken();
		_token.Cid = _cid;
		_token.Pid = _pid;
		// if (string.IsNullOrEmpty(cid))
		// {
		// 	return;
		// }
		// var contains = await _localStorage.ContainKeyAsync($"token.{_cid}");
		// if (contains)
		// {
		// 	var token = await _localStorage.GetItemAsync<BlazorToken>($"token.{_cid}");
		// 	if (token != null)
		// 	{
		// 		_token = token;
		// 	}
		// }
	}
}
