using System.Net;
using System.Net.Sockets;
using terragen_server.DataModels.World;
using terragen_server.Netcode;

namespace terragen_server.DataModels;

public class UserModel
{
    public string Username { get; set; } = string.Empty;
    
    public string UdpAddress { get; set; } = string.Empty;
    
    public string UdpPort { get; set; } = string.Empty;
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public int Z { get; set; }
    
    public float RotationX { get; set; }
    
    public Socket Socket { get; set; }
    
    public CancellationTokenSource Cts { get; set; }
    
    public List<string> ActiveRegions { get; set; } = [];
    
    public List<string> ActiveChunks { get; set; } = [];
    
    
    
    
    private object _tcpLock = new();
    private object _udpLock = new();
    
    public void SendTcpAsync(byte[] data, UserManager userManager)
    {
        lock (_tcpLock)
        {
            try
            {
                Socket.Send(data, SocketFlags.None);
            }
            catch (SocketException e)
            {
                SrvLog.Console.Log("TCP_Listener", $"Error sending data to client: {e.Message}", SrvLog.LogTypes.WARNING);
                this.Cts?.Cancel();
                this.Socket?.Close();
                this.Socket?.Dispose();
                SrvLog.Console.Log("TCP_Listener", $"Client '{this.Username}' disconnected", SrvLog.LogTypes.INFO);
                userManager.RemoveUser(this);
                this.Cts?.Dispose();
                this.ActiveRegions.Clear();
                this.ActiveChunks.Clear();
            }
        }
    }
    
    public async Task SendUdpAsync(byte[] data)
    {
        //TODO: implement UDP send and lock
        
        var endpoint = new IPEndPoint(IPAddress.Parse(UdpAddress), int.Parse(UdpPort));
        var udpClient = new UdpClient();
        udpClient.Connect(endpoint);
        lock (_udpLock)
        {
            try
            {
                udpClient.Send(data, data.Length);
            }
            catch (SocketException e)
            {
                SrvLog.Console.Log("UDP_Listener", $"Error sending data to client: {e.Message}", SrvLog.LogTypes.WARNING);
            }
        }
    }

}