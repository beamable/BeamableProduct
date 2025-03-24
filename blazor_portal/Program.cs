using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using blazor_portal;
using blazor_portal.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorageAsSingleton();
builder.Services.AddSingleton(_sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<IContext, BlazorContext>();
builder.Services.AddSingleton<IAccessToken, BlazorToken>();
builder.Services.AddSingleton<BlazorRequester, BlazorRequester>();
builder.Services.AddSingleton<IBeamableRequester>(provider => provider.GetRequiredService<BlazorRequester>());
builder.Services.AddSingleton<IRequester>(provider => provider.GetService<BlazorRequester>());

builder.Services.AddSingleton<IAuthSettings, DefaultAuthSettings>();
builder.Services.AddSingleton<IAuthApi, AuthApi>();
builder.Services.AddSingleton<IAliasService, AliasService>();
builder.Services.AddSingleton<IRealmsApi, RealmsService>();


await builder.Build().RunAsync();
