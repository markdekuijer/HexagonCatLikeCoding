using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnbasedManager : MonoBehaviour
{
    public static TurnbasedManager Instance;

    public HexGrid grid;
    public bool playerTurn;

    int currentTurn;

    public List<HexUnit> enemyUnits = new List<HexUnit>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Update ()
    {
        if (playerTurn)
        {

        }
        else
        {
            for (int i = 0; i < enemyUnits.Count; i++)
            {
            }
            InitNextTurn();
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
    }
}
