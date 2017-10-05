using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkTestConnector : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {

    

    }
	
	// Update is called once per frame
	void Update () {
		
	}
    /*unity p2p needed commands in the right setup*/
    void IDontDoAnything()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        int myReiliableChannelId = config.AddChannel(QosType.Reliable);
        int myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, 4);
        int hostId = NetworkTransport.AddHost(topology, 8888);
        byte error;
        var connectionId = NetworkTransport.Connect(hostId, "192.16.7.21", 8888, 0, out error);
     
        NetworkTransport.Disconnect(hostId, connectionId, out error);
        int bufferLength = 100;
        byte[] buffer = new byte[bufferLength];
        NetworkTransport.Send(hostId, connectionId, myReiliableChannelId, buffer, bufferLength, out error);

    }
}
