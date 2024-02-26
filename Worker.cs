using LettuceEncrypt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace yarpprox;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _configpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxyconfig.json");
    private readonly string _certpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certpath");

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

	  var webOpts = new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory };

        var builder = WebApplication.CreateBuilder(webOpts);
        builder.Services.AddSystemd(); 
        builder.Services.AddLettuceEncrypt().PersistDataToDirectory(new DirectoryInfo(_certpath), "certpassword");
        
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            var appServices = kestrel.ApplicationServices;
            kestrel.UseSystemd().ListenAnyIP(80);
            kestrel.UseSystemd().ListenAnyIP(443, portopts =>
            {
                portopts.UseHttps(https => { https.UseLettuceEncrypt(appServices); });
            });

        });


        builder.Configuration.AddJsonFile(_configpath, false, false);
        builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
        theWebApp = builder.Build();
        theWebApp.UseDefaultFiles();
        theWebApp.UseStaticFiles(); // The middleware runs before routing happens => no route was found

        theWebApp.UseRouting();
        theWebApp.MapReverseProxy();

        theWebApp.MapFallbackToFile("/spa/{**catch-all}", "index.html");
    }

    private WebApplication theWebApp { get; set; }


    public override async void Dispose()
    {
       await theWebApp.DisposeAsync();
    }


    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await theWebApp.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await theWebApp.StopAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

}
