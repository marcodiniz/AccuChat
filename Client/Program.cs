using AccuChat.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddHttpClient("AccuChat.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// Supply HttpClient instances that include access tokens when making requests to the server project
//builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AccuChat.ServerAPI"));


builder.Services.AddLogging(b =>
{
    b.AddFilter("Microsoft", LogLevel.Warning)
     .AddFilter("System", LogLevel.Warning)
     .AddFilter("", LogLevel.Information);  
});

builder.Services.AddSingleton<GameServer>();

await builder.Build().RunAsync();
