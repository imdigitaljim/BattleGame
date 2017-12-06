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

    private readonly int MAX_PLAYERS = 4;
    [HideInInspector]
    public int CurrentPlayerId = 1, SessionPlayerMax = 2;

    private int this_player_port;

    [HideInInspector]
    public bool IsConnected = false;
    public bool AllMapsReceived = false;
    public bool AllPeersReady = false;
    [HideInInspector]
    public float MaxLongWait = 5.0f, MaxShortWait = 2.0f;
    [HideInInspector]
    private ChatManager Chat;
    private MapSpawn Spawner;
    private UserActionManager User;

    void Start()
    {
        Application.runInBackground = true;
        Chat = gameObject.GetComponent<ChatManager>();
        Spawner = gameObject.GetComponent<MapSpawn>();
        User = gameObject.GetComponent<UserActionManager>();
        InitData();

    }
    void Update()
    {
        if (IsConnected)
        {
            ReceiveData();
            if (!AllMapsReceived)
                Wait_GetMapsFromPeers();
        }
    }

    private float CurrentWait = 0;
    void Wait_CheckReady()
    {
        CurrentWait += Time.deltaTime;
        if (CurrentWait <= MaxShortWait) return;
        CurrentWait = 0;
        SendData("", READY);
        foreach (var entry in ReadyChecks)
        {
            if (!entry.Value) return;
        }
        AllPeersReady = true; 
    }

    void Wait_GetMapsFromPeers()
    {
        CurrentWait += Time.deltaTime;
        if (CurrentWait <= MaxLongWait) return;
        for (int i = 1; i <= SessionPlayerMax; i++)
        {
            if (!MapSeeds.ContainsKey(i))
            {
                //DebugLog(string.Format("Missing Player {0} ", i));
                SendData("", REQUEST, new List<int>(){ i } );
            }

        }
        CurrentWait = 0;
    }

    public void DebugLog(string message)
    {
        Debug.LogFormat(message);
        Chat.AppendNewText(message);

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
    private Dictionary<int, int> PeerFds = new Dictionary<int, int>();
    private Dictionary<int, int> MapSeeds = new Dictionary<int, int>();
    private Dictionary<int, bool> ReadyChecks = new Dictionary<int, bool>();
    private Dictionary<int, int> ConnectionIdTable = new Dictionary<int, int>(); //id : player#

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
        IsConnected = AllMapsReceived = AllPeersReady = false;
        byte error;
        foreach(var entry in PeerFds)
        {
            DebugLog("disconnecting from player "+ entry.Key + " fd=" + entry.Value);
            NetworkTransport.Disconnect(hostId, entry.Value, out error);

        }
        ReadyChecks.Clear();
        NetworkTransport.RemoveHost(hostId);
        Spawner.Clear();
        MapSeeds.Clear();
        PeerFds.Clear();
        ConnectionIdTable.Clear();
        User.Reset();
        //Chat.Clear();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 40, 150, 50), string.Format("Selected Player: {0} of {1}", CurrentPlayerId, SessionPlayerMax));
        if (!IsConnected)
        {
            GUI.Label(new Rect(10, 10, 150, 50), string.Format("Max Players: {0}", SessionPlayerMax));
            SessionPlayerMax = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(25, 25, 100, 30), SessionPlayerMax, 2, MAX_PLAYERS));
            CurrentPlayerId = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(25, 55, 100, 30), CurrentPlayerId, 1, SessionPlayerMax));

            if (GUI.Button(new Rect(40, 80, 50, 30), "Select") )
            {
                /* local use*/
                this_player_port = ALL_PORTS[CurrentPlayerId];
                Bind();
                OpenToPeers();
                
            }
        }
        else
        {
            if (GUI.Button(new Rect(140, 10, 60, 30), "Reset"))
            {
                Reset();
            }
        }
    }



    void OpenToPeers()
    {
      //  DebugLog("trying to connect...");
        byte error;
        for(int i = 1; i <= SessionPlayerMax; i++)
        {
            if (i == CurrentPlayerId) continue;
            PeerFds[i] = NetworkTransport.Connect(hostId, "127.0.0.1", ALL_PORTS[i], 0, out error);
            // DebugLog(string.Format("Connected to topology. fd: {0} for player {1}", peer_fds[i], i));
            ReadyChecks[i] = false;
        }
        IsConnected = true;
        MapSeeds[CurrentPlayerId] = Spawner.SpawnMap(CurrentPlayerId, SessionPlayerMax);
        DebugLog("Awaiting Peer Connections!"); 
    }

    public void SendData(string message, byte type, List<int> players = null)
    {
        try
        {
           // DebugLog(string.Format("Sending: {0} [{1}] ", Convert.ToInt32(type), message));
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
                    NetworkTransport.Send(hostId, PeerFds[i], channelId, sendBuffer, sendBuffer.Length, out error);
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
        foreach (var entry in PeerFds)
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
        SendData(string.Format("{0}|{1}", CurrentPlayerId, MapSeeds[CurrentPlayerId]), SYNC);
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error)
    {
        if (ConnectionIdTable.ContainsKey(connectionId))
        {
            DebugLog(string.Format("Player {0} disconnected (id={1})", ConnectionIdTable[connectionId], connectionId));
            ReadyChecks[ConnectionIdTable[connectionId]] = false;
            ConnectionIdTable.Remove(connectionId);
        }
    }

    void OnData(int hostId, int connectionId, int channelId, byte[] buffer, int size, NetworkError error)
    {
        //DebugLog("===============================================\nReceived:");
        ProcessBuffer(buffer, connectionId);
    }

   
 

    public const byte CHAT = 0;
    public const byte OTHER = 1;
    public const byte SYNC = 2;
    public const byte REQUEST = 3;
    public const byte ATTACK = 4;
    public const byte READY = 5;
    public const byte OUTCOME = 6;


    public string GetPlayersNotReady()
    {
        var playersNotReady = string.Empty;
        foreach (var entry in ReadyChecks)
        {
            if (!entry.Value)
            {
                playersNotReady += string.Format("[Player {0}]", entry.Key);
            }
        }
        return playersNotReady;
    }

    private void ProcessBuffer(byte[] buffer, int connectionId)
    {
        string message = Encoding.ASCII.GetString(buffer, 1, 1023).Replace("\0", string.Empty);
        
        //DebugLog(string.Format("CODE {0} MESSAGE< {1} >", Convert.ToInt32(buffer[0]), message));

        string[] packet;
        int from_peer;
        int seed;
        int i;
        List<Vector3> hits = new List<Vector3>();
        switch (buffer[0])
        {
            case CHAT:
                //DebugLog("CHAT\n===============================================");
                DebugLog(message);
                break;
            case SYNC:
                //DebugLog("MAP\n===============================================");
                packet = message.Split('|');
                Int32.TryParse(packet[0], out from_peer);
                Int32.TryParse(packet[1], out seed);
                ConnectionIdTable[connectionId] = from_peer;
                if (MapSeeds.ContainsKey(from_peer) && MapSeeds[from_peer] == seed) break;
                DebugLog(string.Format("Player {0} is connected.(id={1})[seed={2}]", from_peer, connectionId, seed)); 
                MapSeeds[from_peer] = seed;
                Spawner.SpawnMap(from_peer, SessionPlayerMax, seed);
                for (i = 1; i <= SessionPlayerMax; ++i)
                {
                    if (!MapSeeds.ContainsKey(i)) 
                    {
                        DebugLog(string.Format("Still Waiting for Player {0} to Connect!", i));
                        break;
                    }
                }
                AllMapsReceived = i > SessionPlayerMax;
                if (AllMapsReceived)
                {
                    DebugLog("All Players Connected!");
                    Spawner.SetActive(true);
                    User.InitializeGame();
                }
                //send READY if received all
                break;
            case REQUEST:
                //DebugLog("REQUEST\n===============================================");   
                // from_peer = ConnectionIdTable[connectionId];
                from_peer = ConnectionIdTable[connectionId];
                SendData(string.Format("{0}|{1}", CurrentPlayerId, MapSeeds[CurrentPlayerId]), SYNC, new List<int>() { from_peer });
                break;
            case READY:
                ReadyChecks[ConnectionIdTable[connectionId]] = true;
                DebugLog(string.Format("Player {0} is now ready!", ConnectionIdTable[connectionId]));
                var notReady = GetPlayersNotReady();
                if (notReady != string.Empty)
                {
                    DebugLog("Waiting on " + notReady);
                }
                else
                {
                    AllPeersReady = true;
                    if (User.Ready())
                    {
                        User.Begin();
                    }
                }
                 break;
            case ATTACK:
                //string.Format("{0}|{1}|{2}", id, point.x, point.z)3
                from_peer = ConnectionIdTable[connectionId];
                packet = message.Split('|');
                int to_peer;
                Vector3 attackPosition = new Vector3();
                Int32.TryParse(packet[0], out to_peer);
                float.TryParse(packet[1], out attackPosition.x);
                float.TryParse(packet[2], out attackPosition.z);
                if (CurrentPlayerId == to_peer)
                {
                    hits = User.ReceiveAttack(attackPosition);
                    SendData(SerializeHits(hits), OUTCOME);
                }
                User.IncrementTurn();
                break;
            case OUTCOME:
                from_peer = ConnectionIdTable[connectionId];
                var hitcount = 0;
                packet = message.Split('|');
                Int32.TryParse(packet[0], out hitcount);    
                for (i = 0; i < hitcount * 3; )
                {
                    Vector3 hit = new Vector3();
                    float.TryParse(packet[++i], out hit.x);
                    float.TryParse(packet[++i], out hit.y);
                    float.TryParse(packet[++i], out hit.z);
                    //DebugLog("deserialized to: " + hit.ToString());
                    User.PlaceDestroyedUnit(from_peer, hit);
                }
                if (hitcount == 0)
                {
                    DebugLog("Attack Missed!");
                }
                else
                {
                    DebugLog(string.Format("You Destroyed a Unit of Player {0}!", from_peer));
                }
                User.IncrementTurn();
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


    private string SerializeHits(List<Vector3> hits)
    {
        string result = string.Format("{0}", hits.Count);
        foreach (var hit in hits)
        {
            result += string.Format("|{0:N2}|{1:N2}|{2:N2}", hit.x, hit.y, hit.z);
        }
       // DebugLog("hits were " + result);
        return result;
    }

}
