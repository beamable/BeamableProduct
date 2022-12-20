using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using beamWasm;
using beamWasm.BeamBlazor;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IAppContext, BlazorAppContext>();
builder.Services.AddScoped<IRealmsApi, RealmsService>(); 
builder.Services.AddScoped<IAuthSettings, DefaultAuthSettings>();
builder.Services.AddScoped<IAuthApi, AuthApi>();
builder.Services.AddScoped<IBeamableRequester, BlazorRequester>();

await builder.Build().RunAsync();
