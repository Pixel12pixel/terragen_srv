using terragen_server.DataModels;
using terragen_server.Map;

namespace terragen_server.Netcode;

public class UserManager(MapManager mapManager)
{
    private const int RenderDistance = 16;
    
    private Dictionary<string, int> _loadedRegions = [];
    
    private List<UserModel> _users = [];

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
                _loadedRegions.Remove(region);
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
        
        //TODO: coordinates and chunk load
        
    }

    /// <summary>
    /// Update the user's position and load/unload regions based on their new position.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="rot">X axis rotation</param>
    /// <param name="user">User model</param>
    public void UpdatePosition(int x, int y, int z, float rot, UserModel user)
    {
        var index = _users.IndexOf(user);
        if (index != -1)
        {
            _users[index].X = x;
            _users[index].Y = y;
            _users[index].Z = z;
            _users[index].RotationX = rot;
        }

        var userChunkX = Math.DivRem(x, 16, out _) - (x < 0 ? 1 : 0);
        var userChunkZ = Math.DivRem(z, 16, out _) - (z < 0 ? 1 : 0);

        var regionCorner1X = Math.DivRem(userChunkX + RenderDistance, 16, out _) - ((userChunkX + RenderDistance) < 0 ? 1 : 0);
        var regionCorner1Z = Math.DivRem(userChunkZ + RenderDistance, 16, out _) - ((userChunkZ + RenderDistance) < 0 ? 1 : 0);

        var regionCorner2X = Math.DivRem(userChunkX - RenderDistance, 16, out _) - ((userChunkX - RenderDistance) < 0 ? 1 : 0);
        var regionCorner2Z = Math.DivRem(userChunkZ - RenderDistance, 16, out _) - ((userChunkZ - RenderDistance) < 0 ? 1 : 0);

        var newRegions = Enumerable.Range(regionCorner1X, regionCorner2X - regionCorner1X + 1)
            .SelectMany(i => Enumerable.Range(regionCorner1Z, regionCorner2Z - regionCorner1Z + 1)
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
        
    }
}