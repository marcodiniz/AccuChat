using AccuChat.Server;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog.Events;
using Serilog;
using Serilog.Filters;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            //.WriteTo.Logger(lc => lc.Filter.ByExcluding(le => Matching.FromSource("Microsoft")))
            .WriteTo.Logger(lc =>
                lc.Filter.ByExcluding(Matching.FromSource("Microsoft"))
                    .WriteTo.File("logs/log.txt"))
            .CreateLogger();
Log.Debug("LoggerReady");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(Log.Logger);

builder.Services.AddSingleton<GameStore>();
builder.Services.AddSignalR();
builder.Services.AddCors();

var app = builder.Build();

app.MapHub<ServerHub>("/hub");

#if DEBUG
app.UseCors(c =>
{
    c.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod();
});
#endif

app.Logger.LogInformation("starting");

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
