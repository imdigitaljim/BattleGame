using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    public GameObject gameMapPrefab;
    public Dictionary<int, GameObject> _maps = new Dictionary<int, GameObject>();
    public NetworkConnectionP2P net;
    private Dictionary<int, Vector2> _mapSizes = new Dictionary<int, Vector2>();
    private Dictionary<int, List<Vector2>> _mapPositions = new Dictionary<int, List<Vector2>>();
    // Use this for initialization
    void Start ()
    {
        net = gameObject.GetComponent<NetworkConnectionP2P>();
        _mapSizes[2] = new Vector2(250, 100);
        _mapSizes[3] = new Vector2(250, 80);
        _mapSizes[4] = new Vector2(125, 100);
        _mapPositions[2] = new List<Vector2>() { Vector2.zero, new Vector2(850,635), new Vector2(850, -560)};
        _mapPositions[3] = new List<Vector2>() { Vector2.zero, new Vector2(850, 730), new Vector2(850, 85), new Vector2(850, -635) };
        _mapPositions[4] = new List<Vector2>() { Vector2.zero, new Vector2(300, 730), new Vector2(1600, 730), new Vector2(300, -635), new Vector2(1600, -635) };
    }
	public int SpawnMap(int i, int max_players, int seed = -1)
    {
        net.DebugLog(string.Format("spawning map for player {0} of {1} with seed of {2}", i, max_players, seed));
        if (_maps.ContainsKey(i))
            Destroy(_maps[i]);
        _maps[i] = Instantiate(gameMapPrefab);
        _maps[i].GetComponent<MapInteraction>().ConnectToNetwork(net, i);
        return _maps[i].GetComponent<MapGenerator>().SetNewMap(_mapSizes[max_players], _mapPositions[max_players][i], seed);
    }
    public void Clear()
    {
        foreach (var entry in _maps)
        {
            if (entry.Value != null)
                Destroy(entry.Value);
        }
        _maps.Clear();
    }
	// Update is called once per frame
	void Update () {
		
	}
}
