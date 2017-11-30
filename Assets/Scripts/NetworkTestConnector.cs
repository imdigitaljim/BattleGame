using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;


public class NetworkTestConnector : MonoBehaviour {


    public bool isHost = false;
    private int connectionId, channelId, hostId;
    private string player_1 = "", player_2 = "";
    void Start()
    {
        // Init Transport using default values.
        NetworkTransport.Init();

        // Create a connection config and add a Channel.
        ConnectionConfig config = new ConnectionConfig();
        channelId = config.AddChannel(QosType.Reliable);

        // Create a topology based on the connection config.
        HostTopology topology = new HostTopology(config, 10);

        // Create a host based on the topology we just created, and bind the socket to port 12345.
        hostId = isHost ? NetworkTransport.AddHost(topology, 12345) : NetworkTransport.AddHost(topology, 54321);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), "Network Test Options");
        if (GUI.Button(new Rect(140, 5, 60, 30), "Connect"))
        {
            Debug.Log("Connecting...");
            Connect();
        }
        if (GUI.Button(new Rect(220, 5, 80, 30), "Send Data"))
        {
            Debug.Log("sending...");
            SendData("hello_world");
        }
        player_1 = GUI.TextField(new Rect(10, 40, 150, 20), player_1, 25);
        player_2 = GUI.TextField(new Rect(210, 40, 150, 20), player_2, 25);

    }
    void Update()
    {
        ReceiveData();
    }

    void Connect()
    {
            Debug.Log("trying to connect...");
            // Connect to the host with IP 10.0.0.42 and port 54321
            byte error;
            connectionId = isHost ? NetworkTransport.Connect(hostId, "127.0.0.1", 54321, 0, out error)
          : NetworkTransport.Connect(hostId, "127.0.0.1", 12345, 0, out error);

            Debug.Log("Connected to server. ConnectionId: " + connectionId);
    }
    void SendData(string message)
    {
        if (message.Length * 2 > 2048)
        {
            Debug.LogError("message to large...");
            return;
        }
            
        var buffer = new byte[2048];
        Encoding.ASCII.GetBytes(message).CopyTo(buffer, 0);
        byte error;
        NetworkTransport.Send(hostId, connectionId, channelId, buffer, buffer.Length, out error);
    }
    void ReceiveData()
    {
        var buffer = new byte[2048];
        int outHostId, outConnectionId, outChannelId, receivedSize;
        byte error;
        NetworkEventType evt = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, buffer, buffer.Length, out receivedSize, out error);
        switch (evt)
        {
            case NetworkEventType.Nothing: break;
            case NetworkEventType.ConnectEvent:
            {
                OnConnect(outHostId, outConnectionId, (NetworkError)error);
                break;
            }
            case NetworkEventType.DisconnectEvent:
            {
                OnDisconnect(outHostId, outConnectionId, (NetworkError)error);
                break;
            }
            case NetworkEventType.DataEvent:
            {
                OnData(outHostId, outConnectionId, outChannelId, buffer, receivedSize, (NetworkError)error);
                Debug.Log("buffer received");
                Debug.Log(buffer);
                break;
            }
            case NetworkEventType.BroadcastEvent:
            {
                OnBroadcast(outHostId, buffer, receivedSize, (NetworkError)error);
                break;
            }
            default:
                Debug.LogError("Unknown network message type received: " + evt);
                break;
        }

    }


    void OnConnect(int hostId, int connectionId, NetworkError error)
    {
        Debug.Log("OnConnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error)
    {
        Debug.Log("OnDisconnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
    }

    void OnBroadcast(int hostId, byte[] data, int size, NetworkError error)
    {
        Debug.Log("OnBroadcast(hostId = " + hostId + ", data = "
            + data + ", size = " + size + ", error = " + error.ToString() + ")");
    }

    void OnData(int hostId, int connectionId, int channelId, byte[] data, int size, NetworkError error)
    {
        Debug.Log("OnDisconnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", channelId = " + channelId + ", data = "
            + data + ", size = " + size + ", error = " + error.ToString() + ")");
    }
}
