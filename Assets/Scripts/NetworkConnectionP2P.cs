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
        isConnected = receivedAll = false;
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
        ConnectionIdTable.Clear();
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
               // DebugLog("I'm missing a map from Player: " + i);
               // SendData(string.Format("{0}", i), REQUEST, new List<int>(){ i } );
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
      //  DebugLog("trying to connect...");
        byte error;
        for(int i = 1; i <= session_player_max; i++)
        {
            if (i == this_player_id) continue;
            peer_fds[i] = NetworkTransport.Connect(hostId, "127.0.0.1", ALL_PORTS[i], 0, out error);
           // DebugLog(string.Format("Connected to topology. fd: {0} for player {1}", peer_fds[i], i));
        }
        isConnected = true;
        map_seeds[this_player_id] = _spawner.SpawnMap(this_player_id, session_player_max);
    }

    public void SendData(string message, byte type, List<int> players = null)
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
            if (players == null)
            {
                Broadcast(sendBuffer);
            }
            else
            {
                foreach (var i in players)
                {
                    NetworkTransport.Send(hostId, peer_fds[i], channelId, sendBuffer, sendBuffer.Length, out error);
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogError("EXCEPTIONS" + e.ToString());
            // when trying to chat before its ready!
        }

    }


    public void Broadcast(byte[] buffer)
    {
        byte error; 
        foreach (var entry in peer_fds)
        {
            //DebugLog("broadcasting to: " + entry.Value);
            NetworkTransport.Send(hostId, entry.Value, channelId, buffer, buffer.Length, out error);
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
        //DebugLog("OnConnect(hostId = " + hostId + ", connectionId = "
        //    + connectionId + ", error = " + error.ToString() + ")");
        SendData(string.Format("{0}|{1}", this_player_id, map_seeds[this_player_id]), SYNC);
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error)
    {
        if (ConnectionIdTable.ContainsKey(connectionId))
        {
            DebugLog(string.Format("Player {0} disconnected (id={1})", ConnectionIdTable[connectionId], connectionId));
            ConnectionIdTable.Remove(connectionId);
        }
    }

    void OnData(int hostId, int connectionId, int channelId, byte[] buffer, int size, NetworkError error)
    {
        DebugLog("===============================================\nReceived:");
        ProcessBuffer(buffer, connectionId);
    }

   
    Dictionary<int, int> ConnectionIdTable = new Dictionary<int, int>(); //id : player#

    const int ANY = -1;

    const byte CHAT = 0;
    const byte OTHER = 1;
    const byte SYNC = 2;
    const byte REQUEST = 3;
    
    private void ProcessBuffer(byte[] buffer, int connectionId)
    {
        string message = Encoding.ASCII.GetString(buffer, 1, 1023).Replace("\0", string.Empty);
        
       // DebugLog(string.Format("CODE {0} MESSAGE< {1} >", Convert.ToInt32(buffer[0]), message));

        string[] map_info;
        int from_peer, seed;
        switch (buffer[0])
        {
            case CHAT:
                //DebugLog("CHAT\n===============================================");
                DebugLog(message);
                break;
            case SYNC:
                //DebugLog("MAP\n===============================================");
                map_info = message.Split('|');
                Int32.TryParse(map_info[0], out from_peer);
                Int32.TryParse(map_info[1], out seed);
                ConnectionIdTable[connectionId] = from_peer;
                if (map_seeds.ContainsKey(from_peer) && map_seeds[from_peer] == seed) break;
                DebugLog(string.Format("Player {0} is connected.(id={1})", from_peer, connectionId));
                map_seeds[from_peer] = seed;
                _spawner.SpawnMap(from_peer, session_player_max, seed);
                int i;
                for (i = 1; i <= session_player_max; ++i)
                {
                    if (!map_seeds.ContainsKey(i)) 
                    {
                        DebugLog("Still missing player : " + i);
                        break;
                    }
                }
                receivedAll = i > session_player_max;
                break;
            case REQUEST:
                //DebugLog("REQUEST\n===============================================");         
                Int32.TryParse(message, out from_peer);
                SendData(string.Format("{0}|{1}", this_player_id,  map_seeds[this_player_id]), SYNC, new List<int>() { from_peer });
                break;
            case OTHER:
                //DebugLog("OTHER\n===============================================");
                // do something
                break;
            default:
                //DebugLog("DEFAULT\n===============================================");
                // do something
                break;
        }
    }
}
