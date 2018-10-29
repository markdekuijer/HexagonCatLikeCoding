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
            print("enemy Moving");
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                enemyUnits[i].CalculateNextMove(grid, allyUnits);
            }
            enemyTurned = true;
            //InitNextTurn();
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
            }
            print("playerTurnStart");
            currentTurn++;
        }
        else
        {
            print("enemyTurnStart");
        }
        enemyTurned = false;
    }

    public HexUnit GetClosestAlly(HexCoordinates coord, List<HexUnit> units)
    {
        int MaxInt = int.MaxValue;
        HexUnit u = null;
        for (int i = 0; i < allyUnits.Count; i++)
        {
            if(allyUnits[i].Location.coordinates.DistanceTo(coord) < MaxInt)
            {
                if(units.Contains(allyUnits[i]))
                    u = allyUnits[i];
            }
        }

        return u;
    }
}
