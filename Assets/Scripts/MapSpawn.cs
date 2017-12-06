using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpawn : MonoBehaviour
{
    const int PLAYER1 = 1;
    const int PLAYER2 = 2;
    const int PLAYER3 = 3;
    const int PLAYER4 = 4;
    const int TWO_PLAYER_MATCH = 2;
    const int THREE_PLAYER_MATCH = 3;
    const int FOUR_PLAYER_MATCH = 4;

    public GameObject GameMapPrefab;
    public Dictionary<int, GameObject> MapObjects = new Dictionary<int, GameObject>();
    private NetworkConnectionP2P net;
    private Dictionary<int, Vector2> MapSizes = new Dictionary<int, Vector2>();
    private Dictionary<int, Dictionary<int, Vector2>> MapPositions = new Dictionary<int, Dictionary<int, Vector2>>();
    // Use this for initialization
    void Start ()
    {
        net = gameObject.GetComponent<NetworkConnectionP2P>();
        MapSizes[TWO_PLAYER_MATCH] = new Vector2(250, 100);
        MapSizes[THREE_PLAYER_MATCH] = new Vector2(250, 80);
        MapSizes[FOUR_PLAYER_MATCH] = new Vector2(125, 100);
        MapPositions[TWO_PLAYER_MATCH] = new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(850, 635) },
            { PLAYER2, new Vector2(850, -560) }
        };
        MapPositions[THREE_PLAYER_MATCH] = new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(850, 730) },
            { PLAYER2, new Vector2(850, 85) },
            { PLAYER3, new Vector2(850, -635) }
        };
        MapPositions[FOUR_PLAYER_MATCH] = new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(300, 730) },
            { PLAYER2, new Vector2(1600, 730) },
            { PLAYER3, new Vector2(300, -635) },
            { PLAYER4, new Vector2(1600, -635) }
        };
    }
	public int SpawnMap(int playerId, int max_players,  int seed = -1)
    {
        //net.DebugLog(string.Format("spawning map for player {0} of {1} with seed of {2}", i, max_players, seed));
        if (MapObjects.ContainsKey(playerId))
            Destroy(MapObjects[playerId]);
        MapObjects[playerId] = Instantiate(GameMapPrefab);
        MapObjects[playerId].GetComponent<MapInteraction>().ConnectToNetwork(net, playerId);
        MapObjects[playerId].SetActive(false);
        var positions = MapPositions[max_players][playerId];
        return MapObjects[playerId].GetComponent<MapGenerator>().SetNewMap(MapSizes[max_players], positions, seed, playerId);
    }
    public void SetActive(bool value)
    {
        foreach (var map in MapObjects)
        {
            map.Value.SetActive(value);
        }
    }
    public void Clear()
    {
        foreach (var entry in MapObjects)
        {
            if (entry.Value != null)
                Destroy(entry.Value);
        }
        MapObjects.Clear();
    }
	// Update is called once per frame
	void Update () {
		
	}
}
