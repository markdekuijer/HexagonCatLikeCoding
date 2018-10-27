using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnbasedManager : MonoBehaviour
{
    public static TurnbasedManager Instance;

    public HexGrid grid;
    public bool playerTurn;

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
                enemyUnits[i].CalculateNextMove(grid);
            }
            enemyTurned = true;
            //InitNextTurn();
        }
	}

    public void InitNextTurn()
    {
        playerTurn = !playerTurn;
        if (playerTurn)
        {
            for (int i = 0; i < grid.units.Count; i++)
            {
                print("Reset");
                grid.units[i].hasMovedThisTurn = false;
                grid.units[i].hasAttackThisTurn = false;
            }
            print("next turn (enemyTurnStart)");
            currentTurn++;
        }
        else
        {
            print("next turn (playerTurnStart)");
        }
        enemyTurned = false;
    }

    public HexUnit GetClosestAlly(HexCoordinates coord)
    {
        int MaxInt = int.MaxValue;
        HexUnit u = null;
        for (int i = 0; i < allyUnits.Count; i++)
        {
           if(allyUnits[i].Location.coordinates.DistanceTo(coord) < MaxInt)
            {
                u = allyUnits[i];
            }
        }

        return u;
    }
}
