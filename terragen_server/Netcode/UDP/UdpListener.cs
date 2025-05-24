using System.Net;
using System.Net.Sockets;
using terragen_server.Netcode.UDP.DataModels;

namespace terragen_server.Netcode.UDP;

public class UdpListener(int port, UserManager userManager)
{

    private readonly ProtoSerializeUdp _protoSerializeUdp = new ProtoSerializeUdp();
    
    public async Task StartAsync(TaskCompletionSource readySignal, CancellationTokenSource cts)
    {

        var udpServer = new UdpClient(port);
        
        SrvLog.Console.Log("UDP_Listener", $"UDP Listener started on port {port}", SrvLog.LogTypes.INFO);
        
        readySignal.SetResult();
        
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var result = await udpServer.ReceiveAsync();
                _ = Task.Run(() => HandleClientAsync(result));
            }
            catch (Exception ex)
            {
                SrvLog.Console.Log("UDP_LISTENER",ex.Message.ToString(), SrvLog.LogTypes.ERROR);
            }
        }
    }
    
    /// <summary>
    /// Handles incoming UDP messages from clients.
    /// </summary>
    /// <param name="result">message from client</param>
    private async Task HandleClientAsync(UdpReceiveResult result)
    {
        var user = userManager.GetUserByEndPoint(result.RemoteEndPoint);
        
        if (user == null)
        {
            SrvLog.Console.Log("UDP_Listener", $"Client '{result.RemoteEndPoint}' not found", SrvLog.LogTypes.WARNING);
            return;
        }

        var message = _protoSerializeUdp.Deserialize(result.Buffer);
        
        if (message == null)
        {
            SrvLog.Console.Log("UDP_Listener", $"Failed to deserialize message from '{result.RemoteEndPoint}'", SrvLog.LogTypes.WARNING);
            return;
        }
        
        if (message is PositionUpdateModel positionUpdate)
        {
            if (user.Username != positionUpdate.Username)
            {
                SrvLog.Console.Log("UDP_Listener", $"Username mismatch from '{result.RemoteEndPoint}' - Expected: {user.Username}, Received: {positionUpdate.Username}", SrvLog.LogTypes.WARNING);
                return;
            }
            
            SrvLog.Console.Log("UDP_Listener", $"Position update from '{result.RemoteEndPoint}' - X: {positionUpdate.X}, Y: {positionUpdate.Y}, Z: {positionUpdate.Z}", SrvLog.LogTypes.DEBUG);
            
            userManager.UpdatePosition(positionUpdate.X, positionUpdate.Y, positionUpdate.Z, positionUpdate.RotationX, user);
        }
        else
        {
            SrvLog.Console.Log("UDP_Listener", $"Unknown message type from '{result.RemoteEndPoint}'", SrvLog.LogTypes.WARNING);
        }
        
    }
}