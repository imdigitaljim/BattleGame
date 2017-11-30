using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapInteraction : MonoBehaviour
{

    private NetworkConnectionP2P net;
    private Texture2D _texture;
    public int id;
	// Use this for initialization
	void Start () {
        _texture = gameObject.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
    }
	
	// Update is called once per frame
	void Update ()
    {

           
    }
    public void ConnectToNetwork(NetworkConnectionP2P _net, int _id)
    {
        net = _net;
        id = _id;
      //  net.DebugLog(string.Format("I belong to {0} set by {1}", id, _id));
    }


}
