using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnbasedManager : MonoBehaviour
{
    public static TurnbasedManager Instance;

    public HexGrid grid;
    public GameObject endTurnButton;
    public bool playerTurn;
    bool enemyTurned;

    int currentTurn;

    public List<HexUnit> allyUnits = new List<HexUnit>();
    public List<HexUnit> enemyUnits = new List<HexUnit>();
    public List<HexCell> enemySpawns = new List<HexCell>();

    private bool allowSpawn;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        enemyTurned = true;
        playerTurn = true;
    }

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.P))
            allowSpawn = true;

        if (playerTurn)
        {
            endTurnButton.SetActive(true);
        }
        else
        {
            if (enemyTurned)
                return;

            endTurnButton.SetActive(false);
            StartCoroutine(GoThroughEnemys());
            enemyTurned = true;
        }
    }

    public void InitNextTurn()
    {
        playerTurn = !playerTurn;
        if (playerTurn)
        {
            for (int i = 0; i < allyUnits.Count; i++)
            {
                allyUnits[i].hasMovedThisTurn = false;
                allyUnits[i].hasAttackThisTurn = false;
                allyUnits[i].hasTurned = false;
            }
            print("playerTurnStart");
            HexGameUI.instance.CloseSelect();
            currentTurn++;
        }
        else
        {
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                enemyUnits[i].hasMovedThisTurn = false;
                enemyUnits[i].hasAttackThisTurn = false;
                enemyUnits[i].hasTurned = false;
            }
            print("enemyTurnStart");
        }
        enemyTurned = false;
    }
    IEnumerator GoThroughEnemys()
    {
        if (allowSpawn)
        {
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                if (Random.Range(0, 1) > 0.15)
                    continue;

                int index = Random.Range(0, 5);
                HexUnit u = Instantiate(HexGameUI.instance.unitTypes.unitTypeIDs[index].GetComponent<HexUnit>());
                u.Initialize(index, enemySpawns[i], true);
                u.Grid = grid;
                u.Location = enemySpawns[i];
                TurnbasedManager.Instance.enemyUnits.Add(u);
            }
        }
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i].unitType.objectName == "castle")
                continue;
            enemyUnits[i].CalculateNextMove(grid, allyUnits);
            //while (!enemyUnits[i].hasTurned)
            //{
                yield return null;
            //}
        }
        InitNextTurn();
    }

    public HexUnit GetClosestAlly(HexCoordinates coord, List<HexUnit> units)
    {
        int MaxInt = int.MaxValue;
        HexUnit u = null;
        print(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            if(units[i].Location.coordinates.DistanceTo(coord) < MaxInt)
            {
                u = units[i];
                MaxInt = units[i].Location.coordinates.DistanceTo(coord);
            }
        }

        return u;
    }
}
