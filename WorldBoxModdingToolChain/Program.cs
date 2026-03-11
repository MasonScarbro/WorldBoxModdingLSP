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
            string asmPath = FindAssemblyPath();

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
                            services.AddSingleton(new GameCodeMetaDataRender(asmPath));
                            services.AddSingleton(new ClassDecompiler(asmPath));
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
        /// <summary>
        /// Attempts to locate Assembly-CSharp.dll by checking, in order:
        ///   1. Steam's default 32-bit install path
        ///   2. Steam's default 64-bit install path
        ///   3. Every Steam library folder listed in libraryfolders.vdf
        ///   4. A WORLDBOX_PATH environment variable (for CI / non-standard installs)
        /// Logs each attempt and throws a clear exception if nothing is found.
        /// </summary>
        public static string FindAssemblyPath()
        {
            const string relativeAssembly = @"worldbox\worldbox_Data\Managed\Assembly-CSharp.dll";

            // 1. Common Steam install roots
            var steamRoots = new List<string>
            {
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Steam\steamapps\common",
            };

            // 2. Additional Steam library folders from libraryfolders.vdf
            steamRoots.AddRange(GetSteamLibraryFolders());

            foreach (string root in steamRoots)
            {
                string candidate = Path.Combine(root, relativeAssembly);
                FileLogger.Log($"[FindAssemblyPath] Checking: {candidate}");
                if (File.Exists(candidate))
                {
                    FileLogger.Log($"[FindAssemblyPath] Found at: {candidate}");
                    return candidate;
                }
            }

            // 3. Environment variable override (useful for custom installs or CI)
            string envPath = Environment.GetEnvironmentVariable("WORLDBOX_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                string candidate = Path.Combine(envPath, "worldbox_Data", "Managed", "Assembly-CSharp.dll");
                FileLogger.Log($"[FindAssemblyPath] Checking WORLDBOX_PATH env: {candidate}");
                if (File.Exists(candidate))
                {
                    FileLogger.Log($"[FindAssemblyPath] Found via WORLDBOX_PATH: {candidate}");
                    return candidate;
                }
            }

            // Nothing found — fail loudly so the user knows exactly what to fix
            string message =
                "Could not locate Assembly-CSharp.dll for WorldBox.\n" +
                "Checked standard Steam paths and all Steam library folders.\n" +
                "Set the WORLDBOX_PATH environment variable to your WorldBox install directory and restart.";

            FileLogger.Log($"[FindAssemblyPath] ERROR: {message}");
            throw new FileNotFoundException(message);
        }

        /// <summary>
        /// Reads Steam's libraryfolders.vdf to find any extra Steam library locations
        /// the user has configured (e.g. games installed on a secondary drive).
        /// Returns an empty list if the file cannot be found or parsed.
        /// </summary>
        private static List<string> GetSteamLibraryFolders()
        {
            var folders = new List<string>();

            // libraryfolders.vdf lives in the default Steam install
            var vdfCandidates = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf",
                @"C:\Program Files\Steam\steamapps\libraryfolders.vdf",
            };

            foreach (string vdfPath in vdfCandidates)
            {
                if (!File.Exists(vdfPath)) continue;

                try
                {
                    foreach (string line in File.ReadAllLines(vdfPath))
                    {
                        // Lines look like:   "path"   "D:\\SteamLibrary"
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase)) continue;

                        // Extract the value between the second pair of quotes
                        int first = trimmed.IndexOf('"', 6);         // skip past "path"
                        int second = trimmed.IndexOf('"', first + 1);
                        int third = trimmed.IndexOf('"', second + 1);
                        if (first < 0 || second < 0 || third < 0) continue;

                        string folderPath = trimmed.Substring(second + 1, third - second - 1)
                                                    .Replace(@"\\", @"\");

                        string steamappsCommon = Path.Combine(folderPath, "steamapps", "common");
                        if (Directory.Exists(steamappsCommon))
                        {
                            folders.Add(steamappsCommon);
                            FileLogger.Log($"[GetSteamLibraryFolders] Found library: {steamappsCommon}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"[GetSteamLibraryFolders] Failed to parse {vdfPath}: {ex.Message}");
                }

                break; // Only need the first vdf that exists
            }

            return folders;
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
