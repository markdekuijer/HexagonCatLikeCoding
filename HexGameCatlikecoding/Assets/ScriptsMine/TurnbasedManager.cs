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

    private void Awake()
    {
        print("reeeee");
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
        print("enemy Moving");
        print(enemyUnits.Count);
        for (int i = 0; i < enemyUnits.Count; i++)
        {
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
