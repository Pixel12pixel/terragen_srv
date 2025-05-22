using System.Net;
using System.Net.Sockets;
using terragen_server.DataModels;
using terragen_server.Map;
using terragen_server.Netcode.TCP.DataModels;

namespace terragen_server.Netcode.TCP;

public class TcpListener(int port, int udpPort, MapManager mapManager, UserManager userManager)
{
    private const int PacketSize = 1024 * 4;
    
    private const int Version = 1;
    
    private readonly ProtoSerialize _protoSerialize = new ProtoSerialize(PacketSize);
    
    /// <summary>
    /// Starts the TCP listener.
    /// </summary>
    /// <param name="readySignal">signal for supervisor</param>
    /// <param name="cts">Cancellation token source</param>
    public async Task StartAsync(TaskCompletionSource readySignal, CancellationTokenSource cts)
    {
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endpoint);
            listener.Listen(2);

            SrvLog.Console.Log("TCP_Listener", $"TCP Listener started on port {port}", SrvLog.LogTypes.INFO);
        
            readySignal.SetResult();
        
            while (!cts.IsCancellationRequested)
            {
                var client = await listener.AcceptAsync(); //TODO: add cts
            
                _ = HandleClientAsync(client);
            }
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TCP_Listener", $"Error in connection listener", SrvLog.LogTypes.ERROR);
            readySignal.SetException(e);
        }
    }
    
    private async Task HandleClientAsync(Socket client)
    {
        var buffer = new byte[PacketSize];
        
        var user = await ClientAuthAsync(client);
        
        if (user == null)
        {
            SrvLog.Console.Log("TCP_Listener", $"Client '{client.RemoteEndPoint}' authentication failed", SrvLog.LogTypes.WARNING);
            client.Close();
            return;
        }
        //TODO: add user to list, user manager
        
        userManager.AddUser(user);
        
        SrvLog.Console.Log("TCP_Listener", $"Client '{user.Username}' authenticated from {user.Socket.RemoteEndPoint}", SrvLog.LogTypes.INFO);
        
        _ = Task.Run(() => ListenerLoop(user));
    }

    /// <summary>
    /// Authenticates the client and returns a UserModel.
    /// </summary>
    /// <param name="client">client socket</param>
    /// <returns>User model</returns>
    private async Task<UserModel?> ClientAuthAsync(Socket client)
    {
        var buffer = new byte[PacketSize];

        var user = new UserModel();
        
        try
        {
            var bytesRead = await client.ReceiveAsync(buffer, SocketFlags.None);
            
            var login = _protoSerialize.Deserialize(buffer, bytesRead);

            if (login is LoginModel loginModel)
            {
                if (loginModel.version != Version)
                {
                    SrvLog.Console.Log("TCP_Listener", $"Client version '{loginModel.version}' is not supported, expected '{Version}'", SrvLog.LogTypes.WARNING);
                    return null;
                }
            
                var loginRespondModel = new LoginRespondModel()
                {
                    success = true,
                    udpPort = udpPort
                };
            
                var loginRespondBytes = _protoSerialize.Serialize(loginRespondModel, MessageType.LOGIN_RESPONSE);

                if (loginRespondBytes.Count == 1)
                {
                    await client.SendAsync(loginRespondBytes[0], SocketFlags.None);
                }

                user.Username = loginModel.Username;
                user.UdpAddress = loginModel.UdpAddress;
                user.X = 0;
                user.Y = 0;
                user.Z = 0;
                user.RotationX = 0;
                user.Socket = client;
            
                SrvLog.Console.Log("TCP_Listener", $"Client '{user.Username}' connected from '{client.RemoteEndPoint}'", SrvLog.LogTypes.INFO);

                return user;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    /// <summary>
    /// main listener loop for the user
    /// </summary>
    /// <param name="user">user model with necessary information</param>
    private async Task ListenerLoop(UserModel user)
    {
        var buffer = new byte[PacketSize];

        try
        {
            while (!user.Cts.IsCancellationRequested)
            {
                Array.Clear(buffer, 0, buffer.Length);
                
                var readBytes = await user.Socket.ReceiveAsync(buffer, SocketFlags.None); //TODO: add cts
            
                var message = _protoSerialize.Deserialize(buffer, readBytes);

                if (message is null) 
                    continue; //fragmented message, not all packets received yet

                if (message is DisconnectModel disconnectModel)
                {
                    SrvLog.Console.Log("TCP_Listener", $"Client '{user.Username}' disconnected from {user.Socket.RemoteEndPoint}", SrvLog.LogTypes.INFO);
                    await user.Cts.CancelAsync();
                    userManager.RemoveUser(user);
                    user.Socket.Close();
                    user.Socket.Dispose();
                }
                else if (message is BlockUpdateModel blockUpdateModel)
                {
                    mapManager.UpdateChunk(blockUpdateModel.chunkX, blockUpdateModel.chunkZ, blockUpdateModel.blockX, blockUpdateModel.blockY, blockUpdateModel.blockZ, blockUpdateModel.blockId);
                }
                else if (message is ChatMessageModel chatMessageModel)
                {
                    //temporarly disabled
                    continue;
                }
                else
                {
                    SrvLog.Console.Log("TCP_Listener", $"Unknown message type from '{user.Username}': {user.Socket.RemoteEndPoint}", SrvLog.LogTypes.WARNING);
                }
            
            }
        }
        catch (Exception e)
        {
            SrvLog.Console.Log("TCP_Listener", $"Error while reading from '{user.Username}': {e.Message}", SrvLog.LogTypes.WARNING);
            await user.Cts.CancelAsync();
            userManager.RemoveUser(user);
            user.Socket.Close();
            user.Socket.Dispose();
            SrvLog.Console.Log("TCP_Listener", $"Client '{user.Username}' disconnected", SrvLog.LogTypes.INFO);
        }
        
    }

    /// <summary>
    /// Loops through the user list and checks if any of them are disconnected.
    /// </summary>
    /// <param name="cts">Cancellation Token</param>
    public async Task DisconnectCheckLoop(CancellationTokenSource cts)
    {
        while (!cts.IsCancellationRequested)
        {
            var users = userManager.GetUsers();

            for (var i = 0; i < users.Count; i++)
            {
                var user = users[i];

                try
                {
                    if (user.Socket.Poll(1000, SelectMode.SelectRead) && user.Socket.Available == 0)
                    {
                        SrvLog.Console.Log("TCP_Listener", $"Client '{user.Username}' disconnected", SrvLog.LogTypes.INFO);
                        await user.Cts.CancelAsync();
                        userManager.RemoveUser(user);
                        user.Socket.Close();
                        user.Socket.Dispose();
                        i--;
                    }
                }
                catch (Exception e)
                {
                    SrvLog.Console.Log("TCP_Listener", $"Error while polling {user.Socket.RemoteEndPoint}: {e.Message}", SrvLog.LogTypes.WARNING);
                    userManager.RemoveUser(user);
                    i--;
                }
            }

            await Task.Delay(5000, cts.Token);
        }
    }
}