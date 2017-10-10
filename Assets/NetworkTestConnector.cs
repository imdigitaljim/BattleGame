using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;


public class NetworkTestConnector : MonoBehaviour {


    public bool isHost = false;
    private int connectionId, channelId, hostId;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Connect();
        }
        ReceiveData();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendData();
        }
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
    void SendData()
    {
  
            Debug.Log("Sending....");
            var message = "hello";
            var buffer = new byte[1024];
            Encoding.ASCII.GetBytes(message).CopyTo(buffer, 0);
            byte error;
            NetworkTransport.Send(hostId, connectionId, channelId, buffer, buffer.Length, out error);

    }
    void ReceiveData()
    {
        var buffer = new byte[1024];
        int outHostId, outConnectionId, outChannelId;
        int receivedSize;
        byte error;
        NetworkEventType evt = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, buffer, buffer.Length, out receivedSize, out error);

        switch (evt)
        {
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
            case NetworkEventType.Nothing:
                break;

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
    /*unity p2p needed commands in the right setup*/
    void IDontDoAnything()
    {
        //NetworkTransport.Init();
        //ConnectionConfig config = new ConnectionConfig();
        //int myReiliableChannelId = config.AddChannel(QosType.Reliable);
        //int myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
        //HostTopology topology = new HostTopology(config, 4);
        //int hostId = NetworkTransport.AddHost(topology, 8888);
        //byte error;
        //var connectionId = NetworkTransport.Connect(hostId, "192.16.7.21", 8888, 0, out error);
     
        //NetworkTransport.Disconnect(hostId, connectionId, out error);
        //int bufferLength = 100;
        //byte[] buffer = new byte[bufferLength];
        //NetworkTransport.Send(hostId, connectionId, myReiliableChannelId, buffer, bufferLength, out error);

    }
}
