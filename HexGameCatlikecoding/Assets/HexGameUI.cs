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
            if (Input.GetMouseButtonDown(0))
            {
                DoSelect();
            }
            else if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(1))
                    DoMove(selectedUnit.Speed);
                else
                    DoPathfinding();
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
            selectedUnit = currentCell.Unit;
            cellsToHighlights.Clear();
            cellsToHighlights = grid.SearchMovementArea(selectedUnit.Location, selectedUnit.Speed); //selectedUnit.Speed);
        }
    }

    void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell && selectedUnit.IsValidDestination(currentCell))
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit, cellsToHighlights);
            else
                grid.ClearPath();
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
        }
    }
}
