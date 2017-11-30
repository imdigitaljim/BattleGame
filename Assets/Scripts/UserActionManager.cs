using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserActionManager : MonoBehaviour {


 
    private NetworkConnectionP2P net;
    // Use this for initialization
    void Start () {
       
        net = gameObject.GetComponent<NetworkConnectionP2P>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Clicked();
        }
    }
    void Clicked()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit))
        {
            var map_hit = hit.transform.gameObject.GetComponent<MapInteraction>();
            if (map_hit != null)
            {
                net.DebugLog(string.Format("clicked on map of player {0} at {1}", map_hit.id, hit.point));
                //var color = _texture.GetPixel((int)hit.point.x, (int)hit.point.z);
            }
        }
    }
}
