namespace terragen_server.DataModels;

public class ConfigModel
{
    public int TcpPort { get; set; }
    
    public int UdpPort { get; set; }
    
    public string ServerName { get; set; } = string.Empty;
}