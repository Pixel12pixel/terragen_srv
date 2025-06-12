
using System.Net;
using terragen_server.DataModels;
using terragen_server.Map;
using terragen_server.Netcode.TCP.DataModels;
using terragen_server.Netcode.UDP.DataModels;

namespace terragen_server.Netcode;

public class UserManager(MapManager mapManager)
{
    private const int RenderDistance = 16;
    
    private Dictionary<string, int> _loadedChunks = [];
    
    private Dictionary<string, int> _loadedRegions = [];
    
    private List<UserModel> _users = [];
    
    private ProtoSerializeTcp _serializeTcp = new ProtoSerializeTcp(1024 * 16);

    /// <summary>
    /// Gets the list of users.
    /// </summary>
    /// <returns>list of active users</returns>
    public List<UserModel> GetUsers()
    {
        return _users;
    }
    
    /// <summary>
    /// Add a user to the list and update their position.
    /// </summary>
    /// <param name="user"></param>
    public void AddUser(UserModel user)
    {
        _users.Add(user);
        
        UpdatePosition(user.X, user.Y, user.Z, user.RotationX, user);
    }
    
    /// <summary>
    /// Remove a user from the list and unload their regions.
    /// </summary>
    /// <param name="user"></param>
    public void RemoveUser(UserModel user)
    {
        _users.Remove(user);
        
        foreach (var region in user.ActiveRegions)
        {
            if (_loadedRegions.TryGetValue(region, out var count) && --count == 0)
            {
                //_loadedRegions.Remove(region);
                mapManager.RemoveRegion(region);
            }
            else
            {
                _loadedRegions[region] = count;
            }
        }
    }
    
    /// <summary>
    /// [Not Used] Force update a user in the list.
    /// </summary>
    /// <param name="user"></param>
    public void ForceUpdate(UserModel user)
    {
        var index = _users.IndexOf(user);
        if (index != -1)
        {
            _users[index] = user;
        }
    }

    /// <summary>
    /// Update the user's position and load/unload regions based on their new position.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="rot">X-axis rotation</param>
    /// <param name="user">User model</param>
    public void UpdatePosition(float x, float y, float z, float rot, UserModel user)
    {
        var index = _users.IndexOf(user);
        if (index != -1)
        {
            _users[index].X = x;
            _users[index].Y = y;
            _users[index].Z = z;
            _users[index].RotationX = rot;
        }

        foreach (var client in _users)
        {
            if (client == user)
                continue;

            var positionUpdateBroadcast = new PositionUpdateBroadcastModel()
            {
                PlayerName = user.Username,
                X = x,
                Y = y,
                Z = z,
                RotationX = rot
            };

            var messages = _serializeTcp.Serialize(positionUpdateBroadcast, MessageType.POSITION_UPDATE_BROADCAST);

            foreach (var message in messages)
            {
                client.SendTcpAsync(message, this);
            }
        }

        var userChunkX = Math.DivRem((int)x, 16, out _) - (x < 0 ? 1 : 0);
        var userChunkZ = Math.DivRem((int)z, 16, out _) - (z < 0 ? 1 : 0);

        var chunkMinX = userChunkX - RenderDistance;
        var chunkMaxX = userChunkX + RenderDistance;
        var chunkMinZ = userChunkZ - RenderDistance;
        var chunkMaxZ = userChunkZ + RenderDistance;

        
        var regionMinX = Math.DivRem(chunkMinX, 16, out _) - (chunkMinX < 0 ? 1 : 0);
        var regionMaxX = Math.DivRem(chunkMaxX, 16, out _) - (chunkMaxX < 0 ? 1 : 0);
        var regionMinZ = Math.DivRem(chunkMinZ, 16, out _) - (chunkMinZ < 0 ? 1 : 0);
        var regionMaxZ = Math.DivRem(chunkMaxZ, 16, out _) - (chunkMaxZ < 0 ? 1 : 0);

        var rangeX = regionMaxX - regionMinX + 1;
        var rangeZ = regionMaxZ - regionMinZ + 1;

        var newRegions = Enumerable.Range(regionMinX, rangeX)
            .SelectMany(i => Enumerable.Range(regionMinZ, rangeZ)
                .Select(j => $"{i}_{j}"))
            .ToList();


        if (_users[index].ActiveRegions.SequenceEqual(newRegions))
        {
            return;
        }

        var regionsToLoad = newRegions.Except(_users[index].ActiveRegions).ToList();
        var regionsToUnload = _users[index].ActiveRegions.Except(newRegions).ToList();

        foreach (var t in regionsToUnload)
        {
            if (_loadedRegions.TryGetValue(t, out var count) && --count == 0)
            {
                _loadedRegions.Remove(t);
                mapManager.RemoveRegion(t);
            }
            else
            {
                _loadedRegions[t] = count;
            }
        }

        foreach (var t in regionsToLoad)
        {
            if (_loadedRegions.TryGetValue(t, out var count))
            {
                _loadedRegions[t] = count + 1;
            }
            else
            {
                _loadedRegions[t] = 1;
                mapManager.LoadRegion(t);
            }
        }
        
        _users[index].ActiveRegions = newRegions;
        
        var chunkRangeX = chunkMaxX - chunkMinX + 1;
        var chunkRangeZ = chunkMaxZ - chunkMinZ + 1;
        
        var newChunks = Enumerable.Range(chunkMinX, chunkRangeX)
            .SelectMany(i => Enumerable.Range(chunkMinZ, chunkRangeZ)
                .Select(j => $"{i}_{j}"))
            .ToList();
        
        var chunksToLoad = newChunks.Except(_users[index].ActiveChunks).ToList();
        var chunksToUnload = _users[index].ActiveChunks.Except(newChunks).ToList();
        
        foreach (var t in chunksToLoad)
        {
            if (_loadedChunks.TryGetValue(t, out var count))
            {
                _loadedChunks[t] = count + 1;
            }
            else
            {
                _loadedChunks[t] = 1;
                mapManager.GetChunk( int.Parse(t.Split('_')[0]), int.Parse(t.Split('_')[1]));
            }
        }

        foreach (var t in chunksToLoad)
        {
            if(user.Cts.IsCancellationRequested)
                break;
            var chunkData = new ChunkTransmitModel
            {
                Id = t,
                Data = mapManager.GetChunk(int.Parse(t.Split('_')[0]), int.Parse(t.Split('_')[1])).ChunkData
            };
            var messages = _serializeTcp.Serialize(chunkData, MessageType.CHUNK_TRANSMIT);
            foreach (var message in messages)
            {
                if(user.Cts.IsCancellationRequested)
                    break;
                user.SendTcpAsync(message, this);
            }
        }
        
        foreach (var t in chunksToUnload)
        {
            if (_loadedChunks.TryGetValue(t, out var count) && --count == 0)
            {
                _loadedChunks.Remove(t);
            }
            else
            {
                _loadedChunks[t] = count;
            }
        }
    }
    
    /// <summary>
    /// Gets a user by their endpoint.
    /// </summary>
    /// <param name="endPoint">user IPEndPoint</param>
    /// <returns>user model</returns>
    public UserModel? GetUserByEndPoint(IPEndPoint endPoint)
    {
        var udpAddr = endPoint.Address.ToString();
        var udpPort = endPoint.Port.ToString();

        try
        {
            return _users.FirstOrDefault(user => user.UdpAddress.Equals(udpAddr) && user.UdpPort.Equals(udpPort));
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Sends an updated chunk to a users if the chunk is loaded.
    /// </summary>
    /// <param name="user">User model</param>
    /// <param name="chunkX">chunk X position</param>
    /// <param name="chunkZ">chunk Z position</param>
    public void SendUpdatedChunk(UserModel user, int chunkX, int chunkZ)
    {
        var chunkKey = $"{chunkX}_{chunkZ}";
        
        if (_loadedChunks.TryGetValue(chunkKey, out var count) && count > 0)
        {
            var chunkData = mapManager.GetChunk(chunkX, chunkZ);
            {
                var chunkTransmitModel = new ChunkTransmitModel
                {
                    Id = chunkKey,
                    Data = chunkData.ChunkData
                };
                
                var messages = _serializeTcp.Serialize(chunkTransmitModel, MessageType.BLOCK_UPDATE_BROADCAST);

                foreach (var client in _users)
                {
                    foreach (var message in messages)
                    {
                        client.SendTcpAsync(message, this);
                    }
                }
            }
        }
    }
}