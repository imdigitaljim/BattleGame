using System;

public class ConnectionInfo
{
    public string IpAddress;
    public int Port;
    public int PlayerId;
    public ConnectionInfo(string ip, string port, int id)
    {
        IpAddress = ip;
        if (port == string.Empty)
            Port = -1;
        else
        {
            Int32.TryParse(port, out Port);
        }
        PlayerId = id;
    }
}