using AccuChat.ClientClean;
using AccuChat.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddLogging(b =>
{
    b.AddFilter("Microsoft", LogLevel.Warning);
    b.AddFilter("System", LogLevel.Warning);
    b.AddFilter("", LogLevel.Information);  
});

builder.Services.AddSingleton<GameServer>();

await builder.Build().RunAsync();
