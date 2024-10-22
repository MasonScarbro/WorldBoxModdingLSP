
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorldBoxModdingToolChain;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();
