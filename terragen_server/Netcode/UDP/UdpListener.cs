using System.Net;
using System.Net.Sockets;

namespace terragen_server.Netcode.UDP;

public class UdpListener(int port, UserManager userManager)
{
    
    
    public async Task StartAsync(TaskCompletionSource readySignal, CancellationTokenSource cts)
    {
        using var udpClient = new UdpClient(port);
        
        SrvLog.Console.Log("UDP_Listener", $"UDP Listener started on port {port}", SrvLog.LogTypes.INFO);
        
        
    }
}