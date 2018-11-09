using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;
    public UnityEngine.UI.Text text;

    HexCell currentCell;
    HexUnit selectedUnit;

    List<HexCell> cellsToHighlights = new List<HexCell>();
    public static HexGameUI instance;

    public UnitTypeID unitTypes;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    CheckNewSelect();
                    if (CheckAttack(selectedUnit))
                        return;

                    if (!selectedUnit.hasMovedThisTurn)
                        DoMove(selectedUnit.unitType.speed, GetCell());
                }
                else if (!selectedUnit.hasMovedThisTurn && !selectedUnit.IsTraveling)
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
                CloseSelect();
            }
        }
    }
    public void CheckNewSelect()
    {
        if (GetCell())
        {
            if (GetCell().Unit)
            {
                if (!GetCell().Unit.isEnemy)
                {
                    CloseSelect();
                    DoSelect();
                }
            }
        }
    }
    public bool CheckAttack(HexUnit u)
    {
        if (GetCell() && !u.hasAttackThisTurn)
        {
            if (GetCell().Unit)
            {
                if (GetCell().Unit.isEnemy)
                {
                    if (u.Location.coordinates.DistanceTo(GetCell().coordinates) <= u.unitType.attackRange)
                    {
                        DoAttack(GetCell());
                        CloseSelect();
                        return true;
                    }
                }
            }
        }
        return false;
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
    public void CloseSelect()
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
                    cellsToHighlights = grid.SearchMovementArea(selectedUnit.Location, selectedUnit.unitType.speed);
                    for (int i = 0; i < grid.attackableCells.Count; i++)
                    {
                        //grid.attackableCells[i].EnableHighlight(Color.black);
                    }
                }
                else if (!selectedUnit.hasAttackThisTurn)
                {
                    List<HexCell> showAttackRange = new List<HexCell>();
                    showAttackRange = grid.searchAttackArea(selectedUnit.Location, selectedUnit.unitType.attackRange);

                    for (int i = 0; i < showAttackRange.Count; i++)
                    {
                        //showAttackRange[i].EnableHighlight(Color.black);
                    }
                }
                else
                    cellsToHighlights = new List<HexCell>() { currentCell };
            }
        }
    }
    void PrintCellCoord()
    {
        HexCell currentCell = GetCell();
        if (currentCell)
            text.text = currentCell.coordinates.ToString();
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

    bool attackAfter = false;
    void DoMove(int speed, HexCell c)
    {
        if (grid.HasPath)
        {
            for (int i = 0; i < cellsToHighlights.Count; i++)
            {
                cellsToHighlights[i].DisableHighlight();
            }
            cellsToHighlights.Clear();
            selectedUnit.Travel(grid.GetPath(speed),this, c, true);
            grid.ClearPath();
            if (!grid.attackableCells.Contains(currentCell))
            {
                attackAfter = false;
                selectedUnit = null;
            }
            else
                attackAfter = true;
            for (int i = 0; i < grid.attackableCells.Count; i++)
            {
                grid.attackableCells[i].DisableHighlight();
            }
        }

    }

    public void AttackAfterCheck(HexCell c)
    {
        if (attackAfter)
        {
            DoAttack(c);
            print("attacked");
        }
    }
    void DoAttack(HexCell cell)
    {
        if (!cell.Unit)
            return;

        if(cell.coordinates.DistanceTo(currentCell.coordinates) <= selectedUnit.unitType.attackRange)
            selectedUnit.InitAttack(cell, this);
    }

    public void EndTurn()
    {
        TurnbasedManager.Instance.InitNextTurn();
    }
}
