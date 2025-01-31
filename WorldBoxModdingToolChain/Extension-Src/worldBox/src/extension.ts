import { workspace, ExtensionContext } from 'vscode';
import { spawn } from 'child_process'
import { LanguageClient, LanguageClientOptions, ServerOptions, TransportKind } from 'vscode-languageclient/node';
import * as path from 'path';
import * as fs from 'fs';
let client: LanguageClient;

export function activate(context: ExtensionContext) {

    // The server is implemented as a .NET worker project (use dotnet to run the .dll)
    let serverExe = 'dotnet';
    let serverDllPath = "C:\\Users\\Admin\\source\\repos\\WorldBoxModdingLSP\\WorldBoxModdingToolChain\\bin\\Debug\\net8.0\\WorldBoxModdingToolChain.dll";
    const serverModule = context.asAbsolutePath(
        path.join('server', 'build', 'WorldBoxModdingToolChain.dll')
      );
    // Server options for running the LSP as an external process
    let serverOptions: ServerOptions = {
        run: { command: 'dotnet', args: [serverModule], transport: TransportKind.stdio },
        debug: { command: 'dotnet', args: [serverModule], transport: TransportKind.stdio }
    };

    
    // Client options for handling C# documents
    let clientOptions: LanguageClientOptions = {
        // Register the server for the csharp language (C# files)
        documentSelector: [{ scheme: 'file', language: 'csharp' }],
        synchronize: {
            // Optionally synchronize files or settings
            fileEvents: workspace.createFileSystemWatcher('**/*.cs'), // Watch C# files
        }
    };

    // Create the language client and start it
    client = new LanguageClient('worldBoxModdingLSP', 'WorldBox Modding LSP', serverOptions, clientOptions);
    
    // Start the client
    client.start();
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }
    return client.stop();
}
function findServerDll(): string {
    const workspaceFolders = workspace.workspaceFolders;
    if (!workspaceFolders) {
        return "";
    }

    for (const folder of workspaceFolders) {
        const buildPath = path.join(folder.uri.fsPath, 'server', 'build', 'WorldBoxModdingToolChain.dll');
        if (fs.existsSync(buildPath)) {
            return buildPath;
        }
    }

    return "";
}
