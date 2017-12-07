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
    private Dictionary<int, Vector2> MapSizes = new Dictionary<int, Vector2>()
    {
        {TWO_PLAYER_MATCH,  new Vector2(250, 100)},
        {THREE_PLAYER_MATCH, new Vector2(250, 80)},
        {FOUR_PLAYER_MATCH, new Vector2(125, 100) }
    };
    private Dictionary<int, Dictionary<int, Vector2>> MapPositions = new Dictionary<int, Dictionary<int, Vector2>>()
    {
        {TWO_PLAYER_MATCH, new Dictionary<int, Vector2>()
        {
             { PLAYER1, new Vector2(850, 635) },
             { PLAYER2, new Vector2(850, -560) }
        }},
        {THREE_PLAYER_MATCH, new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(850, 810) },
            { PLAYER2, new Vector2(850, -20) },
            { PLAYER3, new Vector2(850, -860) }
        }},
        {FOUR_PLAYER_MATCH,  new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(200, 550) },
            { PLAYER2, new Vector2(1600, 550) },
            { PLAYER3, new Vector2(200, -635) },
            { PLAYER4, new Vector2(1600, -635) }
        }}
    };
    private Dictionary<int, Dictionary<int, Vector2>> LabelPositions = new Dictionary<int, Dictionary<int, Vector2>>()
    { 
        {TWO_PLAYER_MATCH, new Dictionary<int, Vector2>()
        {
             { PLAYER1, new Vector2(-155, 240) },
             { PLAYER2, new Vector2(-155, -150) }
        }},
        {THREE_PLAYER_MATCH, new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(-155, 300) },
            { PLAYER2, new Vector2(-155, 30) },
            { PLAYER3, new Vector2(-155, -240) }
        }},
        {FOUR_PLAYER_MATCH,  new Dictionary<int, Vector2>()
        {
            { PLAYER1, new Vector2(-160, 215) },
            { PLAYER2, new Vector2(295, 215) },
            { PLAYER3, new Vector2(-160, -165) },
            { PLAYER4, new Vector2(295, -165) }
        }}
    };
    private Dictionary<int, GameObject> MapLabels = new Dictionary<int, GameObject>();
    // Use this for initialization
    void Awake ()
    {
        MapLabels[1] = GameObject.Find("Player1Label");
        MapLabels[2] = GameObject.Find("Player2Label");
        MapLabels[3] = GameObject.Find("Player3Label");
        MapLabels[4] = GameObject.Find("Player4Label");
        Debug.Log(MapLabels[1]);
    }
	public int SpawnMap(int playerId, int max_players,  int seed = -1)
    {
        //net.DebugLog(string.Format("spawning map for player {0} of {1} with seed of {2}", i, max_players, seed));
        if (MapObjects.ContainsKey(playerId))
            Destroy(MapObjects[playerId]);
        MapObjects[playerId] = Instantiate(GameMapPrefab);
        MapObjects[playerId].GetComponent<MapInteraction>().SetId(playerId);
        MapObjects[playerId].SetActive(false);
        var positions = MapPositions[max_players][playerId];
        foreach (var label in MapLabels)
        {
            label.Value.SetActive(false);
        }
        for (int i = 1; i <= max_players; i++)
        {
            MapLabels[i].transform.localPosition = LabelPositions[max_players][i];
            MapLabels[i].SetActive(true);

        }
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
