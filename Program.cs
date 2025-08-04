using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Tumbleweed.Components;
using Tumbleweed.Data;

namespace Tumbleweed;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();
        builder.Services.AddHttpClient("Default").SetHandlerLifetime(TimeSpan.FromMinutes(5));
        builder.Services.AddDbContext<AppDbContext>();
        builder.Services.AddScoped<AppState>();

        builder.RootComponents.Add<App>("app");

        var app = builder.Build();

        app.MainWindow
            .SetTitle("Tumbleweed RSS Reader")
            .SetSize(1280, 960);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        context.Database.Migrate();
        scope.Dispose();

        app.Run();
    }
}
