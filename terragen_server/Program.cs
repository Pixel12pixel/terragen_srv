/******************************************************************************
 * File        : Program.cs
 * Project     : terragen_server
 * Author      : Dorian Koehler
 * Created on  : 05.05.2025
 *
 * Description:
 * Main entry point for the Terragen server application.
 *
 * Language: c#
 * Project version: Dev
 *
 * Comments:
 * This file contains the main program logic for starting the Terragen server.
 * 
 *
 * License:
 * Internal use
 *
 *****************************************************************************/

using System.Reflection;
using SrvLog;
using terragen_server.DataModels;
using terragen_server.FilesInput;
using terragen_server.Listeners;
using terragen_server.Map;
using terragen_server.Netcode;
using Console = System.Console;

namespace terragen_server;

internal class Program
{
    private readonly string _configPath = "config/main.json";


    private static void Main() => new Program().AsyncMain().GetAwaiter().GetResult();
    
    private async Task AsyncMain()
    {
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            SrvLog.Console.Log("TERRAGEN_SERVER", $"Unobserved exception: {args.Exception}", LogTypes.ERROR);
            args.SetObserved();
        };
        
        SrvLog.Console.SetLogLevel(LogTypes.INFO);
        
        Console.Title = "Terragen Server";
        
        
        var versionAttribute = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        
        SrvLog.Console.Log("TERRAGEN_SERVER", "Terragen Server", LogTypes.INFO);
        
        SrvLog.Console.Log("TERRAGEN_SERVER", $"Version: {versionAttribute?.InformationalVersion}", LogTypes.INFO);
        
        
        
        //Setting up the config and tasks
        
        var startNext = new TaskCompletionSource();

        var cts = new CancellationTokenSource();

        ConfigModel? config = null;
        
        try
        {
            ConfigSetup configSetup = new();
        
            config = configSetup.Run(startNext, _configPath);
        
            await startNext.Task;

            if (config == null)
            {
                throw new ApplicationException("Config error");
            }
            
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TERRAGEN_SERVER", e.Message, LogTypes.ERROR);
            
            await Task.Delay(5000, cts.Token);
            
            await cts.CancelAsync();
            
            Environment.Exit(1);
        }

        startNext = new TaskCompletionSource();

        var mapManager = new MapManager(config.Seed);
        
        var userManager = new UserManager(mapManager);
        
        
        try
        {
            mapManager.Init();
            
            //try to load / generate a chunk
            
            mapManager.GetChunk(0, 0);
            
            mapManager.GetChunk(0, -1);
            
            mapManager.GetChunk(-1, 0);
            
            mapManager.GetChunk(-1, -1);
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TERRAGEN_SERVER", e.Message, LogTypes.ERROR);
            
            await Task.Delay(5000, cts.Token);
            
            await cts.CancelAsync();
            
            Environment.Exit(1);
        }

        ListenerSupervisor listenerSupervisor = new(config.TcpPort, config.UdpPort, mapManager, userManager);

        try
        {
            _ = listenerSupervisor.StartSupervisor(startNext, cts);
        
            await startNext.Task;
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TERRAGEN_SERVER", e.Message, LogTypes.ERROR);
            
            await Task.Delay(5000, cts.Token);
            
            await cts.CancelAsync();
            
            Environment.Exit(1);
        }
        
        
        
        //Server startup complete
        
        SrvLog.Console.Log("TERRAGEN_SERVER", "Server started successfully", LogTypes.INFO);
        
        await Task.Delay(-1, cts.Token);
    }
}