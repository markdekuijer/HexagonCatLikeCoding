using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnbasedManager : MonoBehaviour
{
    [SerializeField] private HexGrid grid;

    int currentTurn;
	void Start ()
    {
	}
	
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitNextTurn();
        }
	}

    public void InitNextTurn()
    {
        for (int i = 0; i < grid.units.Count; i++)
        {
            grid.units[i].hasAttackThisTurn = false;
            grid.units[i].hasAttackThisTurn = false;
        }
    }
}
