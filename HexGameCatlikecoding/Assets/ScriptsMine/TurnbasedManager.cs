using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnbasedManager : MonoBehaviour
{
    public static TurnbasedManager Instance;

    public HexGrid grid;
    public bool playerTurn = true;

    int currentTurn;

    public List<HexUnit> allyUnits = new List<HexUnit>();
    public List<HexUnit> enemyUnits = new List<HexUnit>();
    public List<HexCell> enemySpawns = new List<HexCell>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    bool enemyTurned;
    void Update ()
    {
        if (playerTurn)
        {

        }
        else
        {
            if (enemyTurned)
                return;

            StartCoroutine(GoThroughEnemys());
            enemyTurned = true;
        }
    }

    private void LateUpdate()
    {
        
    }

    public void InitNextTurn()
    {
        playerTurn = !playerTurn;
        if (playerTurn)
        {
            for (int i = 0; i < grid.units.Count; i++)
            {
                grid.units[i].hasMovedThisTurn = false;
                grid.units[i].hasAttackThisTurn = false;
                grid.units[i].hasTurned = false;
            }
            print("playerTurnStart");
            HexGameUI.instance.CloseSelect();
            currentTurn++;
        }
        else
        {
            print("enemyTurnStart");
        }
        enemyTurned = false;
    }
    IEnumerator GoThroughEnemys()
    {
        if (currentTurn % 4 == 0)
        {
            for (int i = 0; i < enemySpawns.Count; i++)
            {

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
            while (!enemyUnits[i].hasTurned)
            {
                yield return null;
            }
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
