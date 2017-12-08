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
    [HideInInspector]
    public int CurrentPlayerId = 1, PlayerMax = 2;

    private int CurrentPlayerPort;

    [HideInInspector]
    public bool IsConnected = false, AllMapsReceived = false, AllPeersReady = false;
    [HideInInspector]
    private float MaxLongWait = 5.0f, MaxShortWait = 2.0f;
 
    private ChatManager Chat;
    private MapSpawn Spawner;
    private UserActionManager User;
    void Start()
    {
        Application.runInBackground = true;
        Chat = gameObject.GetComponent<ChatManager>();
        Spawner = gameObject.GetComponent<MapSpawn>();
        User = gameObject.GetComponent<UserActionManager>();
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
        SendData("", Protocol.READY);
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
        for (int i = 1; i <= PlayerMax; i++)
        {
            if (!MapSeeds.ContainsKey(i))
            {
                //DebugLog(string.Format("Missing Player {0} ", i));
                SendData("", Protocol.REQUEST, new List<int>(){ i } );
            }

        }
        CurrentWait = 0;
    }

    public void LogChat(string message)
    {
        Debug.LogFormat(message);
        Chat.AppendNewText(message);

    }
    private bool InitNetworkTransport()
    {
        // Init Transport using default values.
        NetworkTransport.Init();

        // Create a connection config and add a Channel.
        ConnectionConfig config = new ConnectionConfig();
        channelId = config.AddChannel(QosType.Reliable);

        // Create a topology based on the connection config.
        HostTopology topology = new HostTopology(config, 10);
       // LogChat(string.Format("using port {0}", CurrentPlayerPort));
        hostId = NetworkTransport.AddHost(topology, CurrentPlayerPort);
       // LogChat(string.Format("hostid = {0}", hostId));
        return hostId < 0;
    }

    
    //this is for local-use only
    private Dictionary<int, int> PortTable = new Dictionary<int, int>()
    {
        {1, 11111}, {2, 11112}, {3, 11113}, {4, 11114}
    };
    private Dictionary<int, int> PeerFds = new Dictionary<int, int>();
    private Dictionary<int, int> MapSeeds = new Dictionary<int, int>();
    private Dictionary<int, bool> ReadyChecks = new Dictionary<int, bool>();
    private Dictionary<int, int> ConnectionIdTable = new Dictionary<int, int>(); //id : player#

    public void Reset()
    {
       // Debug.Log("resetting...");
        IsConnected = AllMapsReceived = AllPeersReady = false;
        byte error;
        foreach(var entry in PeerFds)
        {
            LogChat(string.Format("Disconnecting from Player {0} fd:{1}", entry.Key, entry.Value));
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


    public bool BeginConnect(List<ConnectionInfo> peers, int max, int selected)
    {
        PlayerMax = max;
        CurrentPlayerId = selected;
        CurrentPlayerPort = peers[selected - 1].Port < 0 ? PortTable[selected] : peers[selected - 1].Port;
        //LogChat(string.Format("I'm Player {0} of {1} on port {2}", selected, max, CurrentPlayerPort));
        InitNetworkTransport();
        OpenToPeers(peers);
        LogChat(string.Format("I'm Player {0} of {1} on port {2}", selected, max, CurrentPlayerPort));
        return true;
    }

    void OpenToPeers(List<ConnectionInfo> peers)
    {
      //  DebugLog("trying to connect...");
        byte error;
        for(int i = 1; i <= PlayerMax; i++)
        {
            if (i == CurrentPlayerId) continue;
            var ipaddress = peers[i - 1].IpAddress == string.Empty ? "127.0.0.1" : peers[i - 1].IpAddress;
            var port = peers[i - 1].Port < 0 ? PortTable[i] : peers[i - 1].Port;
            LogChat(string.Format("Connecting to {0}:{1}", ipaddress, port));
            PeerFds[i] = NetworkTransport.Connect(hostId, ipaddress, port, 0, out error);
            // DebugLog(string.Format("Connected to topology. fd: {0} for player {1}", peer_fds[i], i));
            ReadyChecks[i] = false;
        }
        IsConnected = true;
        MapSeeds[CurrentPlayerId] = Spawner.SpawnMap(CurrentPlayerId, PlayerMax);
        LogChat("Awaiting Peer Connections!"); 
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
        //LogChat("OnConnect(hostId = " + hostId + ", connectionId = "
        //    + connectionId + ", error = " + error.ToString() + ")");
        SendData(string.Format("{0}|{1}", CurrentPlayerId, MapSeeds[CurrentPlayerId]), Protocol.SYNC);
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error)
    {
        if (ConnectionIdTable.ContainsKey(connectionId))
        {
            LogChat(string.Format("Player {0} disconnected (id={1})", ConnectionIdTable[connectionId], connectionId));
            ReadyChecks[ConnectionIdTable[connectionId]] = false;
            ConnectionIdTable.Remove(connectionId);
        }
    }

    void OnData(int hostId, int connectionId, int channelId, byte[] buffer, int size, NetworkError error)
    {
        //DebugLog("===============================================\nReceived:");
        ProcessBuffer(buffer, connectionId);
    }

   
 



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
            case Protocol.CHAT:
                //DebugLog("CHAT\n===============================================");
                LogChat(message);
                break;
            case Protocol.SYNC:
                //DebugLog("MAP\n===============================================");
                packet = message.Split('|');
                Int32.TryParse(packet[0], out from_peer);
                Int32.TryParse(packet[1], out seed);
                ConnectionIdTable[connectionId] = from_peer;
                if (MapSeeds.ContainsKey(from_peer) && MapSeeds[from_peer] == seed) break;
                LogChat(string.Format("Player {0} is connected.(id={1})[seed={2}]", from_peer, connectionId, seed)); 
                MapSeeds[from_peer] = seed;
                Spawner.SpawnMap(from_peer, PlayerMax, seed);
                for (i = 1; i <= PlayerMax; ++i)
                {
                    if (!MapSeeds.ContainsKey(i)) 
                    {
                        LogChat(string.Format("Still Waiting for Player {0} to Connect!", i));
                        break;
                    }
                }
                AllMapsReceived = i > PlayerMax;
                if (AllMapsReceived)
                {
                    LogChat("All Players Connected!");
                    Spawner.SetActive(true);
                    User.InitializeGame();
                }
                //send READY if received all
                break;
            case Protocol.REQUEST:
                //DebugLog("REQUEST\n===============================================");   
                // from_peer = ConnectionIdTable[connectionId];
                from_peer = ConnectionIdTable[connectionId];
                SendData(string.Format("{0}|{1}", CurrentPlayerId, MapSeeds[CurrentPlayerId]), Protocol.SYNC, new List<int>() { from_peer });
                break;
            case Protocol.READY:
                ReadyChecks[ConnectionIdTable[connectionId]] = true;
                LogChat(string.Format("Player {0} is now ready!", ConnectionIdTable[connectionId]));
                var notReady = GetPlayersNotReady();
                if (notReady != string.Empty)
                {
                    LogChat("Waiting on " + notReady);
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
            case Protocol.ATTACK:
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
                    SendData(SerializeHits(hits), Protocol.OUTCOME);
                }
                User.IncrementTurn();
                break;
            case Protocol.OUTCOME:
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
                    LogChat("Attack Missed!");
                }
                else
                {
                    LogChat(string.Format("You Destroyed a Unit of Player {0}!", from_peer));
                }
                User.IncrementTurn();
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
