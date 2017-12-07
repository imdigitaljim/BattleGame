using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserActionManager : MonoBehaviour {
     
    private NetworkConnectionP2P net;
    private int MAX_UNITS = 1;
    private List<GameObject> UnitGameObjects = new List<GameObject>();
    private List<GameObject> AttackGameObjects = new List<GameObject>();
    private List<GameObject> ReceivedAttackGameObjects = new List<GameObject>();
    private List<GameObject> DestroyedUnitGameObjects = new List<GameObject>();
    private UIManager UIPanel;
    public GameObject UnitPrefab, AttackPrefab, ReceivePrefab;

    private const int PHASE_PLACEMENT = 0;
    private const int PHASE_AWAITALL = 1;
    private const int PHASE_OTHERTURN = 2;
    private const int PHASE_MYTURN = 3;

    public Dictionary<int, int> HealthCounts = new Dictionary<int, int>();
    public int CurrentPhase = PHASE_PLACEMENT;

    private int CurrentPlayerTurn = 1;

    public void InitializeGame()
    {
        net.LogChat("Click on your map to place units.");
        net.LogChat(string.Format("Place {0} units!", MAX_UNITS - UnitGameObjects.Count));
    }
    public void Begin()
    {
        net.LogChat("Game is Starting!");
        for (int i = 1; i <= net.PlayerMax; ++i)
        {
            HealthCounts[i] = UnitGameObjects.Count;
        }
        PromptTurn();
    }
 
    public void PromptTurn()
    {
        if (net.CurrentPlayerId == CurrentPlayerTurn)
        {
            net.LogChat("Your Turn! Click a Spot on the Enemy Map to Attack!");
            CurrentPhase = PHASE_MYTURN;
        }
        else
        {
            net.LogChat(string.Format("Player {0}'s turn. Wait while they attack!", CurrentPlayerTurn));
            CurrentPhase = PHASE_OTHERTURN;
        }
    }
    public void IncrementTurn()
    {
        CheckConditions();
        int StartTurn = CurrentPlayerTurn;
        for (int i = 0; i < net.PlayerMax; i++)
        {
            CurrentPlayerTurn = (CurrentPlayerTurn % net.PlayerMax) + 1;
            if (HealthCounts[CurrentPlayerTurn] > 0) break;
         }
        if (StartTurn == CurrentPlayerTurn) return;
        CurrentPhase = CurrentPlayerTurn == net.CurrentPlayerId ? PHASE_MYTURN : PHASE_OTHERTURN;
        PromptTurn();
    }
    void CheckConditions()
    {
        bool hasLost = false;
        for (int i = 1; i <= net.PlayerMax; i++)
        {
            if (HealthCounts[i] == 0)
            {
                HealthCounts[i]--;
                if (i == net.CurrentPlayerId)
                {
                    net.LogChat("You lost! All your units are destroyed!");
                    hasLost = true;
                    break;
                }
                else
                {
                    net.LogChat(string.Format("Player {0} has been destroyed!", i));
                    net.LogChat(string.Format("Players Remaining {0}", GetRemainingPlayerCount()));
                }
            }
            if (HealthCounts[i] <= 0 && i == net.CurrentPlayerId)
            {
                hasLost = true;
                break;
            }
        }
        if (GetRemainingPlayerCount() > 1) return;
        StartCoroutine(WaitAndChangeScene(hasLost ? 1 : 2));
    }

    private int GetRemainingPlayerCount()
    {
        int count = 0;
        foreach (var health in HealthCounts)
        {
            if (health.Value > 0)
                count++;
        }
        return count;
    }

    private IEnumerator WaitAndChangeScene(int condition)
    {
        yield return new WaitForSeconds(.5f);
        UIPanel.Reset(condition);
    }

    public bool Ready()
    {
        return CurrentPhase == PHASE_AWAITALL;
    }

    // Use this for initialization
    void Start () {

        UIPanel = GetComponent<UIManager>();
        net = gameObject.GetComponent<NetworkConnectionP2P>();
        
    }

    public void ClearGameObjects(List<GameObject> list)
    {
        foreach (var unit in list)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
    }

    public void Reset()
    {
        ClearGameObjects(UnitGameObjects);
        ClearGameObjects(ReceivedAttackGameObjects);
        ClearGameObjects(AttackGameObjects);
        ClearGameObjects(DestroyedUnitGameObjects);
        CurrentPhase = PHASE_PLACEMENT;
        CurrentPlayerTurn = 1;
        DestroyedUnitGameObjects.Clear();
        UnitGameObjects.Clear();
        ReceivedAttackGameObjects.Clear();
        AttackGameObjects.Clear();
    }
    // Update is called once per frame
    void Update ()
    {
        if (!net.IsConnected) return;
        if (Input.GetMouseButtonDown(0))
        {
            Clicked();
        }
    }

    void Clicked()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit))
        {
            var map_hit = hit.transform.gameObject.GetComponent<MapInteraction>();
            if (map_hit != null)
            {
                hit.transform.gameObject.GetComponent<MapInteraction>().Detect(hit.point);
                ProcessClick(map_hit.id, hit.point);
            }
        }
    }

    void ProcessClick(int id, Vector3 point)
    {
        switch (CurrentPhase)
        {
            case PHASE_AWAITALL:
                break;
            case PHASE_PLACEMENT:
                //net.LogChat(string.Format("clicked on map of player {0} at {1}", id, point));
                PlaceUnit(id, point);
                break;
            case PHASE_OTHERTURN:
                net.LogChat(string.Format("Waiting for other players turns, Current Player {0}", CurrentPlayerTurn));
                break;
            case PHASE_MYTURN:
               // net.DebugLog(string.Format("clicked on map of player {0} at {1}", id, point));
                MakeAttack(id, point);
                break;
        }
    }
    
    public List<Vector3> ReceiveAttack(Vector3 point)
    {
        
        var attack = Instantiate(ReceivePrefab);
        attack.transform.position = point;
        ReceivedAttackGameObjects.Add(attack);

        var hits = new List<Vector3>();
        foreach(var _obj in Physics.OverlapSphere(point, 50))
        {
            if (_obj.gameObject.tag == "Player")
            {
                HealthCounts[net.CurrentPlayerId]--;
                net.LogChat("You lost a unit!");
                _obj.gameObject.tag = "Untagged";
                hits.Add(_obj.gameObject.transform.position);
                _obj.gameObject.GetComponent<Renderer>().material.color = Color.red;

            }    
        }
        return hits;
    }

    void MakeAttack(int id, Vector3 point)
    {
        if (id == net.CurrentPlayerId)
        {
            net.LogChat("This is Your Map, Attack the Opponent!");
        }
        else
        {
            var attack = Instantiate(AttackPrefab);
            attack.transform.position = point;
            AttackGameObjects.Add(attack);
            net.SendData(string.Format("{0}|{1}|{2}", id, point.x, point.z), Protocol.ATTACK, new List<int>() { id });
        }
    }
    
    public void PlaceDestroyedUnit(int id, Vector3 point)
    {
        HealthCounts[id]--;
        var destroyed = Instantiate(UnitPrefab);
        destroyed.transform.position = point;   
        destroyed.GetComponent<Renderer>().material.color = Color.red;
        DestroyedUnitGameObjects.Add(destroyed);
    }

    void PlaceUnit(int id, Vector3 point)
    {
        if (id == net.CurrentPlayerId && UnitGameObjects.Count < MAX_UNITS)
        {
            var next_unit = Instantiate(UnitPrefab);
            next_unit.transform.position = point;
            UnitGameObjects.Add(next_unit);
            if (UnitGameObjects.Count == MAX_UNITS)
            {
                net.LogChat("Unit Placement Complete!");
               
                CurrentPhase = PHASE_AWAITALL;
                net.SendData("", Protocol.READY);
                if (!net.AllPeersReady)
                {
                    net.LogChat("Waiting on Other Player Placements");
                }
                else
                {
                    Begin();
                }
            }
            else
            {
                net.LogChat(string.Format("Place {0} units!", MAX_UNITS - UnitGameObjects.Count));
            }
        }
        else
        {
            net.LogChat("Wrong Map! Place Units on Your Map!");
        }
    }
}
