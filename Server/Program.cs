using AccuChat.Server;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<GameStore>();
builder.Services.AddSignalR();
builder.Services.AddCors();
builder.Services.AddLogging(b =>
{
    b.AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning);
});

var app = builder.Build();

app.MapHub<ServerHub>("/hub");
app.UseCors(c =>
{
    c.AllowAnyOrigin().AllowAnyHeader();
});
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
