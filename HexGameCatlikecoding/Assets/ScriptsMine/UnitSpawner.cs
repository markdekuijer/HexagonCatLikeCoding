using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public HexCell currentSelectedSpawner;
    public HexGrid grid;

    public void SpawnUnit(int unitIndex)
    {
        HexUnit u = Instantiate(HexGameUI.instance.unitTypes.unitTypeIDs[unitIndex].GetComponent<HexUnit>());
        u.Initialize(unitIndex, currentSelectedSpawner, false);
        u.Grid = grid;
        u.Location = currentSelectedSpawner;
        TurnbasedManager.Instance.allyUnits.Add(u);
        gameObject.SetActive(false);
    }
}
