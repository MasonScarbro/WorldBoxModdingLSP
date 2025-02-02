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
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace WorldBoxModdingToolChain
{

    public class Program
    {
        public static bool DEBUG_FLAG = true;
        public static async Task Main(string[] args)
        {
            if (DEBUG_FLAG)
            {
                FileLogger.Initialize(FindLogsFolder("worldbox"), "[Program]"); // For debugging
                FileLogger.Log("Starting LSP server...");
            }
            //TODO: The asm and the decompiled folder path need to be gathered dynamically!
            

            string compiledFolderPath = FindCompiledFolder("worldbox");
            

            try
            {
                
                var server = await LanguageServer.From(options =>
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .WithHandler<TextDocumentHandler>()
                        .WithHandler<CompletionHandler>()
                        //.WithHandler<DiagnosticHandler>()
                        .WithHandler<DefinitionHandler>()
                        

                        .WithServices(services =>
                        {
                            services.AddSingleton(new GreaterSuggestions());
                            services.AddSingleton(new GameCodeMetaDataRender("C:\\Program Files (x86)\\Steam\\steamapps\\common\\worldbox\\worldbox_Data\\Managed\\Assembly-CSharp.dll"));
                            services.AddSingleton(new ClassDecompiler("C:\\Program Files (x86)\\Steam\\steamapps\\common\\worldbox\\worldbox_Data\\Managed\\Assembly-CSharp.dll"));
                            services.AddSingleton<IDictionary<Uri, SourceText>>(new Dictionary<Uri, SourceText>());
                            services.AddSingleton(new AnalysisStorage());
                            services.AddSingleton(new DocumentParserService());
                            services.AddSingleton(new PathLibrary(compiledFolderPath));
                            
                            

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
            FileLogger.Close();
        }

        public static string FindCompiledFolder(string extensionName)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var vscodeExtensionsPath = Path.Combine(userProfile, ".vscode", "extensions");
            
            if (Directory.Exists(vscodeExtensionsPath))
            {
                FileLogger.Log($"{vscodeExtensionsPath} extension path FOUND");
                var extensionFolderPath = Directory
                    .GetDirectories(vscodeExtensionsPath)
                    .FirstOrDefault(dir => dir.Contains(extensionName, StringComparison.OrdinalIgnoreCase));
                if (extensionFolderPath != null)
                {
                    // Assuming compiled folder is under server/
                    var compiledFolderPath = Path.Combine(extensionFolderPath, "server", "compiled");
                    
                    if (Directory.Exists(compiledFolderPath))
                    {
                        FileLogger.Log(compiledFolderPath + " Exists");
                        return compiledFolderPath;
                    }
                }
            }
            //else
            if (Directory.Exists(@$"C:\Users\{Environment.UserName}\.vscode\extensions\masonscarbro.worldbox-0.0.1\server\compiled"))
            {
                FileLogger.Log($"{extensionName} Not Found but It was found as C:\\Users\\Admin\\.vscode\\extensions");
                return @$"C:\Users\{Environment.UserName}\.vscode\extensions\masonscarbro.worldbox-0.0.1\server\compiled";
            }
            FileLogger.Log($"{extensionName} Not Found using default");
            return $"C:\\Users\\Admin\\source\\repos\\WorldBoxModdingLSP\\WorldBoxModdingToolChain\\Decompiled";
        }

        public static string FindLogsFolder(string extensionName)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var vscodeExtensionsPath = Path.Combine(userProfile, ".vscode", "extensions");

            if (Directory.Exists(vscodeExtensionsPath))
            {
                
                var extensionFolderPath = Directory
                    .GetDirectories(vscodeExtensionsPath)
                    .FirstOrDefault(dir => dir.Contains(extensionName, StringComparison.OrdinalIgnoreCase));
                if (extensionFolderPath != null)
                {
                    // Assuming compiled folder is under server/
                    var compiledFolderPath = Path.Combine(extensionFolderPath, "logs");

                    if (Directory.Exists(compiledFolderPath))
                    {
                        
                        return Path.Combine(compiledFolderPath, "debug.txt");
                    }
                }
            }
            //else
            if (Directory.Exists(@$"C:\Users\{Environment.UserName}\.vscode\extensions\masonscarbro.worldbox-0.0.1\logs\"))
            {
                
                return @$"C:\Users\{Environment.UserName}\.vscode\extensions\masonscarbro.worldbox-0.0.1\logs\debug.txt";
            }
            
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\logs\debug.txt";
        }

    }
}
