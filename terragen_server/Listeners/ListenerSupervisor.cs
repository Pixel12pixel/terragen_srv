using System.Collections.Concurrent;
using System.Net;
using SrvLog;
using terragen_server.Map;
using terragen_server.Netcode;
using terragen_server.Netcode.TCP;
using terragen_server.Netcode.UDP;

namespace terragen_server.Listeners;

public class ListenerSupervisor(int tcpPort, int udpPort, MapManager mapManager, UserManager userManager)
{
    private readonly TcpListener _tcpListener = new(tcpPort, udpPort, mapManager, userManager);
    private readonly UdpListener _udpClient = new(udpPort, userManager);
    
    private readonly Dictionary<string, Func<Task>> _taskFactories = new();
    private readonly Dictionary<string, Task> _tasks = new();
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Starts the listeners supervisor.
    /// </summary>
    /// <param name="readySignel">TaskComplerionSource to inform when supervisor and subtasks succesfully started</param>
    /// <param name="cts">Cancellation token to shut down supervisor and TCP/UDP listeners</param>
    public async Task StartSupervisor(TaskCompletionSource readySignel, CancellationTokenSource cts)
    {
        try
        {
            SrvLog.Console.Log("Starting Listeners Supervisor", LogTypes.INFO);
        
            _ = Task.Run(() => SupervisorLoop(cts));
        
            //Register tasks
        
            var tcpReadySignal = new TaskCompletionSource();
            var udpReadySignal = new TaskCompletionSource();
        
            RegisterTask("TcpListener", () => _tcpListener.StartAsync(tcpReadySignal, cts));
            RegisterTask("UdpListener", () => _udpClient.StartAsync(udpReadySignal, cts));
            RegisterTask("TcpDisconnectLoop", () => _tcpListener.DisconnectCheckLoop(cts));
        
            await tcpReadySignal.Task;
            await udpReadySignal.Task;

            if (tcpReadySignal.Task.IsCompleted && udpReadySignal.Task.IsCompleted)
            {
                SrvLog.Console.Log("NET_SUPERVISOR", "All listeners are ready", LogTypes.INFO);
                readySignel.SetResult();
            }
            else
            {
                SrvLog.Console.Log("NET_SUPERVISOR", "One of the listeners is not working", LogTypes.CRITICAL);
                readySignel.SetException(new Exception("One of the listeners is not working"));
            }
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("NET_SUPERVISOR", "Error in Listeners Supervisor!!", LogTypes.CRITICAL);
            SrvLog.Console.Log("NET_SUPERVISOR", e.Message, LogTypes.CRITICAL);
            if (!readySignel.Task.IsCompleted)
            {
                readySignel.SetException(new Exception("Error in Listeners Supervisor!!"));
            }
        }
    }

    /// <summary>
    /// Main loop of the supervisor. Checks if any of the tasks are completed / throw errors and restarts them if they are.
    /// </summary>
    /// <param name="cts">Cancelation token to stop restarting and end loop</param>
    private async Task SupervisorLoop(CancellationTokenSource cts)
    {
        while (!cts.IsCancellationRequested)
        {
            foreach (var kvp in _tasks.ToList())
            {
                var key = kvp.Key;
                var task = kvp.Value;

                if (!task.IsCompleted)
                    continue;

                SrvLog.Console.Log("NET_SUPERVISOR", $"One of the listeners has stopped: {key}. Restarting...", LogTypes.WARNING);

                if (_taskFactories.TryGetValue(key, out var factory))
                {
                    _tasks[key] = factory();
                }
                else
                {
                    SrvLog.Console.Log("NET_SUPERVISOR", "No factory for task...", LogTypes.ERROR);
                }
            }

            await Task.Delay(_checkInterval, cts.Token);
        }
    }

    /// <summary>
    /// Register new task with the supervisor. The task will be started and monitored.
    /// </summary>
    /// <param name="name">Task name</param>
    /// <param name="taskFactory">Task function to run</param>
    private void RegisterTask(string name, Func<Task> taskFactory)
    {
        _taskFactories[name] = taskFactory;
        _tasks[name] = taskFactory();
    }
}