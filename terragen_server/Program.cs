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
    private readonly string _configPath = "config\\main.json";


    private static void Main() => new Program().AsyncMain().GetAwaiter().GetResult();
    
    private async Task AsyncMain()
    {
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
            
            Environment.Exit(1);
        }

        startNext = new TaskCompletionSource();

        var mapManager = new MapManager();
        
        var userManager = new UserManager(mapManager);
        
        
        
        
        UserModel model = new UserModel()
        {
            Username = "a",
            UdpAddress = "123.123.123.123",
            UdpPort = "1234",
            X = 0,
            Y = 0,
            Z = 0,
            RotationX = 0,
        };
        
        try
        {
            //try to load / generate a chunk

            
            
            userManager.AddUser(model);
            
            //mapManager.GetChunk(0, 0);
            
            //mapManager.GetChunk(0, -1);
            
            //mapManager.GetChunk(-1, 0);
            
            //mapManager.GetChunk(-1, -1);
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TERRAGEN_SERVER", e.Message, LogTypes.ERROR);
            
            await Task.Delay(5000, cts.Token);
            
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
            
            Environment.Exit(1);
        }
        
        
        
        //Server startup complete
        
        SrvLog.Console.Log("TERRAGEN_SERVER", "Server started successfully", LogTypes.INFO);
        
        await Task.Delay(3000, cts.Token);
        
        
        
        userManager.UpdatePosition(18, 14, 20, 0, model);
        
        await Task.Delay(-1, cts.Token);
    }
}