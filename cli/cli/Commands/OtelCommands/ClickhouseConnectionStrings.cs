namespace cli.OtelCommands;

public struct ClickhouseConnectionStrings
{
    
    /// <summary>
    /// should not have protocol.
    /// </summary>
    public string Host { get; set; }
    public string Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Endpoint { get; set; }
}
