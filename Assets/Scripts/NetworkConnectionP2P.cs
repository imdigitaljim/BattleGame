using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;

using UnityEngine.UI;
using System;

public class NetworkConnectionP2P : MonoBehaviour
{
    private int connectionId, channelId, hostId;
    public float slider_max = 2.0f;
    private float slider_player = 1.0f;

    private readonly int MAX_PLAYERS = 4;
    public int this_player_id;
    private int session_player_max;
    private int this_player_port;

    private bool isConnected = false;
    private bool receivedAll = false;
    private float max_wait = 5.0f;
    private float current_wait = 0;

    private ChatManager chat;
    private MapSpawn _spawner;


    void Start()
    {
        Application.runInBackground = true;
        chat = gameObject.GetComponent<ChatManager>();
        _spawner = gameObject.GetComponent<MapSpawn>();
        InitData();

    }
    public void DebugLog(string message)
    {
        Debug.LogFormat(message);
        chat.AppendNewText(message);

    }
    private void Bind()
    {
        // Init Transport using default values.
        NetworkTransport.Init();

        // Create a connection config and add a Channel.
        ConnectionConfig config = new ConnectionConfig();
        channelId = config.AddChannel(QosType.Reliable);

        // Create a topology based on the connection config.
        HostTopology topology = new HostTopology(config, 10);

        hostId = NetworkTransport.AddHost(topology, this_player_port);
    }
   


    //this is for local-use only
    private Dictionary<int, int> ALL_PORTS = new Dictionary<int, int>();
    private Dictionary<int, int> peer_fds = new Dictionary<int, int>();
    private Dictionary<int, int> map_seeds = new Dictionary<int, int>();

    private void InitData()
    {
        ALL_PORTS[1] = 11111;
        ALL_PORTS[2] = 11112;
        ALL_PORTS[3] = 11113;
        ALL_PORTS[4] = 11114;
    }

    private void Reset()
    {
        Debug.Log("resetting...");
        isConnected = false;
        byte error;
        foreach(var entry in peer_fds)
        {
            DebugLog("disconnecting from player "+ entry.Key + " fd=" + entry.Value);
            NetworkTransport.Disconnect(hostId, entry.Value, out error);
        }
        NetworkTransport.RemoveHost(hostId);
        _spawner.Clear();
        map_seeds.Clear();
        peer_fds.Clear();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 40, 150, 50), string.Format("Selected Player: {0}", slider_player));
        if (!isConnected)
        {
            GUI.Label(new Rect(10, 10, 150, 50), string.Format("Max Players: {0}", slider_max));
            slider_max = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(25, 25, 100, 30), slider_max, 2, MAX_PLAYERS));
            slider_player = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(25, 55, 100, 30), slider_player, 1, slider_max));

            if (GUI.Button(new Rect(40, 80, 50, 30), "Select") )
            {
                this_player_id = Mathf.RoundToInt(slider_player);
                session_player_max = Mathf.RoundToInt(slider_max);
                DebugLog("Connecting...");
                /* local use*/
                this_player_port = ALL_PORTS[this_player_id];
                Bind();
                Connect();
                
            }
        }
        else
        {
            // GUI.Label(new Rect(10, 10, 100, 20), "Network Test Options");
            if (GUI.Button(new Rect(140, 10, 60, 30), "Reset"))
            {
                Reset();
            }
            //if (GUI.Button(new Rect(220, 10, 80, 30), "Send Data"))
            //{
            //    SendData("hello_world");
            //}
        }



   

    }

    void GetMapsFromPeers()
    {
        current_wait += Time.deltaTime;
        if (current_wait <= max_wait) return;     
        for (int i = 1; i <= session_player_max; i++)
        {
            if (!map_seeds.ContainsKey(i))
            {
                SendData(i.ToString(), REQUEST);
            }

        }
        current_wait = 0;    
    }

    void Update()
    {
        if (isConnected)
        {
            ReceiveData();
            if (!receivedAll)
                GetMapsFromPeers();           
        }
      
    }
    void Connect()
    {
        DebugLog("trying to connect...");
        byte error;
        for(int i = 1; i <= session_player_max; i++)
        {
            if (i == this_player_id) continue;
            DebugLog("using " + ALL_PORTS[i]);
            peer_fds[i] = NetworkTransport.Connect(hostId, "127.0.0.1", ALL_PORTS[i], 0, out error);
            DebugLog(string.Format("Connected to topology. ConnectionFd: {0} for player {1}", peer_fds[i], i));
        }
        isConnected = true;
        map_seeds[this_player_id] = _spawner.SpawnMap(this_player_id, session_player_max);
        DebugLog("my seed is " + map_seeds[this_player_id]);
    }

    public void SendData(string message, byte type)
    {
        try
        {
            DebugLog(string.Format("Sending: {0} [{1}] ", Convert.ToInt32(type), message));
            var sendBuffer = new byte[1024];
            var messageBuffer = Encoding.ASCII.GetBytes(message);
            if (messageBuffer.Length > 1023)
            {
                throw new Exception("sending data too large do something else");
            }
            var typeA = new byte[] { type };
            Buffer.BlockCopy(typeA, 0, sendBuffer, 0, 1);
            Buffer.BlockCopy(messageBuffer, 0, sendBuffer, 1, messageBuffer.Length);
            string newmessage = Encoding.ASCII.GetString(sendBuffer, 1, 1023);
            Debug.Log(newmessage);
            byte error;
            foreach (var entry in peer_fds)
            {
                DebugLog("sending..." + entry.Value);
                NetworkTransport.Send(hostId, entry.Value, channelId, sendBuffer, sendBuffer.Length, out error);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("EXCEPTIONS" + e.ToString());
            // when trying to chat before its ready!
        }

    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];
        int outHostId, outConnectionId, outChannelId;
        int receivedSize;
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
                break;
            }
            case NetworkEventType.BroadcastEvent: break;

            default:
                Debug.LogError("Unknown network message type received: " + evt);
                break;
        }

    }



    void OnConnect(int hostId, int connectionId, NetworkError error)
    {
        DebugLog("OnConnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
        SendData(string.Format("{0}|{1}", this_player_id, map_seeds[this_player_id]), MAP);
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error)
    {
        DebugLog("OnDisconnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
    }

    void OnData(int hostId, int connectionId, int channelId, byte[] buffer, int size, NetworkError error)
    {
        ProcessBuffer(buffer);
    }


    const byte CHAT = 0;
    const byte OTHER = 1;
    const byte MAP = 2;
    const byte REQUEST = 3;
    
    private void ProcessBuffer(byte[] buffer)
    {
        string message = Encoding.ASCII.GetString(buffer, 1, 1023).Replace("\0", string.Empty);
        DebugLog("===============================================\nReceived:");
        DebugLog(string.Format("CODE {0} MESSAGE< {1} >", Convert.ToInt32(buffer[0]), message));

        switch (buffer[0])
        {
            case CHAT:
                DebugLog("CHAT\n===============================================");
                DebugLog(message);
                break;
            case MAP:
                DebugLog("MAP\n===============================================");
                var map_info = message.Split('|');
                DebugLog(string.Format("received map of user {0} of seed {1}", map_info[0], map_info[1]));
                int seed, id;
                Int32.TryParse(map_info[0], out id);
                Int32.TryParse(map_info[1], out seed);
                map_seeds[id] = seed;
                DebugLog("passing in " + id);
                _spawner.SpawnMap(id, session_player_max, seed);
                for(int i = 1; i <= session_player_max; i++)
                {
                    if (!map_seeds.ContainsKey(i)) return;
                }
                receivedAll = true;
                break;
            case REQUEST:
                DebugLog("REQUEST\n===============================================");
                SendData(string.Format("{0}|{1}", this_player_id, map_seeds[this_player_id]), MAP);
                break;
            case OTHER:
                DebugLog("OTHER\n===============================================");
                // do something
                break;
            default:
                DebugLog("DEFAULT\n===============================================");
                // do something
                break;
        }
    }
}
