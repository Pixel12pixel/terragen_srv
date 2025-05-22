using System.Net.Sockets;
using terragen_server.DataModels.World;

namespace terragen_server.DataModels;

public class UserModel
{
    public string Username { get; set; } = string.Empty;
    
    public string UdpAddress { get; set; } = string.Empty;
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public int Z { get; set; }
    
    public float RotationX { get; set; }
    
    public Socket Socket { get; set; }
    
    public CancellationTokenSource Cts { get; set; }
    
    public List<string> ActiveRegions { get; set; } = [];
    
    
    
    
    private object _tcpLock = new();
    private object _udpLock = new();
    
    private async Task SendTcpAsync(byte[] data)
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
            }
        }
    }
    
    public async Task SendUdpAsync(byte[] data)
    {
        //TODO: implement UDP send and lock
    }
}