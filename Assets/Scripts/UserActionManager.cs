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
    public GameObject UnitPrefab;
    public GameObject AttackPrefab;
    public GameObject ReceivePrefab;

    private const int PHASE_PLACEMENT = 0;
    private const int PHASE_AWAITALL = 1;
    private const int PHASE_OTHERTURN = 2;
    private const int PHASE_MYTURN = 3;
    private const int PHASE_GAMECOMPLETE = 4;

    public Dictionary<int, int> HealthCounts = new Dictionary<int, int>();
    public int CurrentPhase = PHASE_PLACEMENT;

    private int CurrentPlayerTurn = 1;

    public void InitializeGame()
    {
        net.DebugLog("Click on your map to place units.");
        net.DebugLog(string.Format("Place {0} units!", MAX_UNITS - UnitGameObjects.Count));
    }
    public void Begin()
    {
        net.DebugLog("Beginning!");
        for (int i = 1; i <= net.SessionPlayerMax; ++i)
        {
            HealthCounts[i] = UnitGameObjects.Count;
        }
        PromptTurn();
    }
 
    public void PromptTurn()
    {
        if (net.CurrentPlayerId == CurrentPlayerTurn)
        {
            net.DebugLog("Your Turn!");
            CurrentPhase = PHASE_MYTURN;
        }
        else
        {
            net.DebugLog(string.Format("Player {0}'s turn.", CurrentPlayerTurn));
            CurrentPhase = PHASE_OTHERTURN;
        }
    }
    public void IncrementTurn()
    {
        CheckConditions();

        int next_turn;
        int test_win = CurrentPlayerTurn;
        for (int i = 0; i < net.SessionPlayerMax; i++)
        {
            next_turn = (CurrentPlayerTurn % net.SessionPlayerMax) + 1;
            if (HealthCounts[next_turn] > 0)
            {
                CurrentPlayerTurn = next_turn;
                break;
            }
        }
        if (CurrentPlayerTurn == test_win) return;
        CurrentPhase = CurrentPlayerTurn == net.CurrentPlayerId ? PHASE_MYTURN : PHASE_OTHERTURN;
        PromptTurn();
    }
    void CheckConditions()
    {

        for (int i = 1; i <= net.SessionPlayerMax; i++)
        {
            if (HealthCounts[i] == 0)
            {
                HealthCounts[i]--;
                if (i == net.CurrentPlayerId)
                {
                    net.DebugLog("You lost! All your units are destroyed!");
                    CurrentPhase = PHASE_GAMECOMPLETE;
                    //TODO: Change to lose screen
                    return;
                }
                else
                {
                    net.DebugLog(string.Format("Player {0} has been destroyed!", i));
                }
            }
        }
        foreach (var health in HealthCounts)
        {
            if (health.Value > 0 && health.Key != net.CurrentPlayerId) return;
        }
        CurrentPhase = PHASE_GAMECOMPLETE;
        net.DebugLog("You destroyed all the opponents! You win!!");
        //TODO: Change to Win Screen
    }

    public bool Ready()
    {
        return CurrentPhase == PHASE_AWAITALL;
    }

    // Use this for initialization
    void Start () {
       
        net = gameObject.GetComponent<NetworkConnectionP2P>();
        
    }
    public void Reset()
    {
        foreach (var unit in UnitGameObjects)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        foreach (var unit in ReceivedAttackGameObjects)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        foreach (var unit in AttackGameObjects)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        foreach (var unit in DestroyedUnitGameObjects)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        CurrentPhase = PHASE_PLACEMENT;
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

        RaycastHit hit = new RaycastHit();

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
                net.DebugLog(string.Format("clicked on map of player {0} at {1}", id, point));
                PlaceUnit(id, point);
                break;
            case PHASE_OTHERTURN:
                net.DebugLog(string.Format("Waiting for other players turns, Current Player {0}", CurrentPlayerTurn));
                break;
            case PHASE_GAMECOMPLETE:
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
                net.DebugLog("You lost a unit!");
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
            net.DebugLog("This is Your Map, Attack the Opponent!");
        }
        else
        {
            var attack = Instantiate(AttackPrefab);
            attack.transform.position = point;
            AttackGameObjects.Add(attack);
            net.SendData(string.Format("{0}|{1}|{2}", id, point.x, point.z), NetworkConnectionP2P.ATTACK);
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
                net.DebugLog("Unit Placement Complete!");
               
                CurrentPhase = PHASE_AWAITALL;
                net.SendData("", NetworkConnectionP2P.READY);
                if (!net.AllPeersReady)
                {
                    net.DebugLog("Waiting on Other Player Placements");
                }
                else
                {
                    Begin();
                }
            }
            else
            {
                net.DebugLog(string.Format("Place {0} units!", MAX_UNITS - UnitGameObjects.Count));
            }
        }
        else
        {
            net.DebugLog("Wrong Map!");
        }
    }
}
