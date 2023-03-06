using AccuChat.Server;
using Microsoft.AspNetCore.ResponseCompression;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameStore>();
builder.Services.AddSignalR();
builder.Services.AddCors();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddLogging(b =>
{
    b.AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning);
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();

var staticOptions = new StaticFileOptions();
staticOptions.OnPrepareResponse = (r) =>
{
    r.Context.Response.Headers["Bypass-Tunnel-Reminder"] = "yes";
};
app.UseStaticFiles(staticOptions);

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ServerHub>("/hub");
//app.UseCors(c =>
//{
//    c.AllowAnyOrigin().AllowAnyHeader();
//});
app.Logger.LogInformation("starting");
app.Use(async (context, next) =>
{
    await next(context);
    context.Response.Headers["Bypass-Tunnel-Reminder"] = "yes";
    context.Request.Headers["Bypass-Tunnel-Reminder"] = "yes";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Run();