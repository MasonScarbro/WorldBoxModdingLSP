using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorldBoxModdingToolChain.Handlers;
using WorldBoxModdingToolChain.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using WorldBoxModdingToolChain.Analysis;
using Microsoft.CodeAnalysis.Text;

namespace WorldBoxModdingToolChain
{
    public class Program
    {   
        
        public static async Task Main(string[] args)
        {
            

            FileLogger.Initialize("C:\\Users\\Admin\\source\\repos\\WorldBoxModdingLSP\\WorldBoxModdingToolChain\\Logs\\debug.txt", "[Program]"); // For debugging
            FileLogger.Log("Starting LSP server...");

            

            try
            {
                var server = await LanguageServer.From(options =>
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .WithHandler<TextDocumentHandler>()
                        .WithHandler<CompletionHandler>()
                        .WithServices(services =>
                        {
                            services.AddSingleton(new GameCodeMetaDataRender("C:\\Program Files (x86)\\Steam\\steamapps\\common\\worldbox\\worldbox_Data\\Managed\\Assembly-CSharp.dll"));
                            services.AddSingleton<IDictionary<Uri, SourceText>>(new Dictionary<Uri, SourceText>());
                            services.AddSingleton(new AnalysisStorage());
                        })
                        .OnInitialize((server, request, token) =>
                        {
                            
                            if (request is InitializeParams initParams)
                            {
                                FileLogger.Log($"ProcessId: {initParams.ProcessId}");
                                FileLogger.Log($"ClientInfo: {initParams.ClientInfo}");
                                // Log other properties as needed
                            }

                            var response = new InitializeResult
                            {
                                Capabilities = new ServerCapabilities
                                {
                                    TextDocumentSync = TextDocumentSyncKind.Full,
                                    HoverProvider = true,
                                    CodeActionProvider = true,
                                },
                                ServerInfo = new ServerInfo
                                {
                                    Name = "WorldBoxLSP",
                                    Version = "1.0.0",
                                }
                            };

                            FileLogger.Log("Response: " + response);
                            
                            return Task.FromResult(response);
                        })
                        .OnStarted((server, token) =>
                        {
                            FileLogger.Log("LSP Server 'Started'");
                            
                            return Task.CompletedTask;
                        })
                ).ConfigureAwait(false);

                await server.WaitForExit.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"An error occurred while running the LSP server. {ex}");
                
            }

            FileLogger.Log($"LSP server ended");
        }
    }
}
