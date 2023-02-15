using AccuChat.Client;
using AccuChat.Shared;
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

await builder.Build().RunAsync();
