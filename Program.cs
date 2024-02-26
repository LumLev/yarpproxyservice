using Microsoft.Extensions.Hosting;
using yarpprox;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSystemd();
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();