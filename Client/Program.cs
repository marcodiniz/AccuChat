using AccuChat.Client;
using AccuChat.Shared;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");


builder.Services.AddLogging(b =>
{
    b.AddFilter("Microsoft", LogLevel.Warning)
     .AddFilter("System", LogLevel.Warning)
     .AddFilter("", LogLevel.Information);  
});

builder.Services.AddSingleton<GameServer>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
