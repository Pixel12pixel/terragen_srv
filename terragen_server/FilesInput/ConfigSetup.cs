using System.Text.Json;
using terragen_server.DataModels;

namespace terragen_server.FilesInput;

public class ConfigSetup
{
    /// <summary>
    /// Get config file from path or create new one with default values.
    /// </summary>
    /// <param name="ready">ready signal for supervisor</param>
    /// <param name="file">config path</param>
    /// <returns>config model</returns>
    public ConfigModel? Run(TaskCompletionSource ready, string file = "config\\main.json")
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), file); //windows
        
        //var path = Path.Combine("/app/data/", file);
        
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                
                if (!string.IsNullOrEmpty(json))
                {
                    var config = JsonSerializer.Deserialize<ConfigModel>(json);
                    
                    SrvLog.Console.Log("CONFIG", "Config file loaded successfully.", SrvLog.LogTypes.INFO);
                    ready.SetResult();
                    return config;
                }
                else
                {
                    json = JsonSerializer.Serialize(GetDefaultConfig(), new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(path, json);
                
                    SrvLog.Console.Log("CONFIG", "Config file empty, creating new one with default values.", SrvLog.LogTypes.INFO);
                
                    ready.SetResult();
                    
                    return GetDefaultConfig();
                }
            }
            catch (Exception e)
            {
                SrvLog.Console.Log("CONFIG", $"Error loading config file: {e.Message}", SrvLog.LogTypes.CRITICAL);
                
                ready.SetException(e);
                
                return null;
            }
        }
        else
        {
            try
            {
                var directory = Path.GetDirectoryName(path) ?? string.Empty;
            
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception e)
            {
                SrvLog.Console.Log("CONFIG", $"Error creating config directory: {e.Message}", SrvLog.LogTypes.CRITICAL);
                
                ready.SetException(e);
                
                return null;
            }
            
            try
            {
                {
                    File.Create(path);
                }
                
                var json = JsonSerializer.Serialize(GetDefaultConfig(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                {
                    File.WriteAllText(path, json);
                }
                
                SrvLog.Console.Log("CONFIG", "Config file not found, creating new one with default values.", SrvLog.LogTypes.INFO);
                
                ready.SetResult();
                
                return GetDefaultConfig();
            }
            catch (Exception e)
            {
                SrvLog.Console.Log("CONFIG", $"Error creating config file: {e.Message}", SrvLog.LogTypes.CRITICAL);
                
                ready.SetException(e);
                
                return null;
            }
        }
    }
    
    /// <summary>
    /// Default config values.
    /// </summary>
    /// <returns>ConfigModel object with default values</returns>
    private ConfigModel GetDefaultConfig()
    {
        return new ConfigModel
        {
            TcpPort = 7777,
            UdpPort = 7778,
            ServerName = "Terragen Server",
            Seed = 1234
        };
    }
}