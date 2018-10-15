using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;

    HexCell currentCell;
    HexUnit selectedUnit;

    List<HexCell> cellsToHighlights = new List<HexCell>();

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!selectedUnit.hasMovedThisTurn)
                        DoMove(selectedUnit.Speed);
                    else if (!selectedUnit.hasAttackThisTurn)
                        DoAttack(GetCell());
                }
                else if (!selectedUnit.hasMovedThisTurn)
                {
                    DoPathfinding();
                }
            }
            else if (Input.GetMouseButtonDown(0) && !selectedUnit)
            {
                DoSelect();
            }

            if (Input.GetMouseButtonDown(1))
            {
                selectedUnit = null;
                for (int i = 0; i < cellsToHighlights.Count; i++)
                {
                    cellsToHighlights[i].DisableHighlight();
                }
                for (int i = 0; i < grid.attackableCells.Count; i++)
                {
                    grid.attackableCells[i].DisableHighlight();
                }
            }
            else if (Input.GetMouseButtonDown(2))
            {
                grid.ClearPath();
                grid.SearchMovementArea(GetCell(), 20);
            }
        }
    }

    public void SetEditMode(bool toggle)
    {
        print("edit");
        enabled = !toggle;
        grid.ShowUI(!toggle);
        grid.ClearPath();
        if (toggle)
        {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        }
        else
        {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }

    bool UpdateCurrentCell()
    {
       HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if(cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    HexCell GetCell()
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        return cell;
    }

    void DoSelect()
    {
        grid.ClearPath();
        UpdateCurrentCell();
        if (currentCell)
        {
            if (currentCell.Unit)
            {
                if (currentCell.Unit.IsTraveling || currentCell.Unit.isEnemy)
                    return;

                selectedUnit = currentCell.Unit;
                cellsToHighlights.Clear();

                if (!selectedUnit.hasMovedThisTurn)
                {
                    cellsToHighlights = grid.SearchMovementArea(selectedUnit.Location, selectedUnit.Speed);
                    for (int i = 0; i < grid.attackableCells.Count; i++)
                    {
                        grid.attackableCells[i].EnableHighlight(Color.black);
                    }
                }
                else if (!selectedUnit.hasAttackThisTurn)
                {
                    List<HexCell> showAttackRange = new List<HexCell>();
                    showAttackRange = grid.searchAttackArea(selectedUnit.Location, selectedUnit.attackRange);

                    for (int i = 0; i < showAttackRange.Count; i++)
                    {
                        showAttackRange[i].EnableHighlight(Color.black);
                    }
                }
                else
                    cellsToHighlights = new List<HexCell>() { currentCell };
            }
        }
    }

    void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell && selectedUnit.IsValidDestination(currentCell))
            {
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit, cellsToHighlights);
            }
            else if (grid.attackableCells.Contains(GetCell()))
            {
                print("can reach shorten path");
            }
            else
            {
                grid.ClearPath();
                for (int i = 0; i < cellsToHighlights.Count; i++)
                {
                    cellsToHighlights[i].EnableHighlight(Color.white);
                }
            }
        }
    }

    void DoMove(int speed)
    {
        if (grid.HasPath)
        {
            for (int i = 0; i < cellsToHighlights.Count; i++)
            {
                cellsToHighlights[i].DisableHighlight();
            }
            cellsToHighlights.Clear();
            selectedUnit.Travel(grid.GetPath(speed));
            grid.ClearPath();
            selectedUnit = null;
            for (int i = 0; i < grid.attackableCells.Count; i++)
            {
                grid.attackableCells[i].DisableHighlight();
            }
        }
    }

    void DoAttack(HexCell cell)
    {
        if (!cell.Unit)
            return;

        if(cell.coordinates.DistanceTo(currentCell.coordinates) <= selectedUnit.attackRange)
            selectedUnit.InitAttack(cell);
    }
}
