using OmniSharp.Extensions.LanguageServer.Server;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Reactive;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using WorldBoxModdingToolChain.Handlers;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ILanguageServer _server;
        
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            FileLogger.Initialize("C:\\Users\\Admin\\source\\repos\\WorldBoxModdingLSP\\WorldBoxModdingToolChain\\Logs\\debug.txt", "[Worker]"); //NOT the logger just for debugging

        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting LSP server...");

            try
            {
                _server = await LanguageServer.From(options =>
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .OnInitialize((server, request, token) =>
                        {
                            _logger.LogInformation("LSP Server Initialized");
                            return Task.CompletedTask;
                        })
                        
                        .WithHandler<TextDocumentHandler>()
                        .WithHandler<HoverHandler>()
                        .WithHandler<CompletionHandler>()
                        .OnStarted((server, token) =>
                        {
                            _logger.LogInformation("LSP Server Initialized");
                            return Task.CompletedTask;
                        })
                );
                await _server.WaitForExit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the LSP server.");
            }
            _logger.LogInformation("LSP server stopping...");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Log the stop event
            _logger.LogInformation("LSP server is shutting down...");

            // Dispose the server if it's still running
            if (_server != null)
            {
                await _server.WaitForExit;
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
