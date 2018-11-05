using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Material terrainMaterial;

    public HexGrid hexGrid;
    int activeTerrainTypeIndex;

    int activeElevation;
    bool applyElevation;

    int activeWaterLevel;
    bool applyWaterLevel;

    int activeUrbanLevel;
    bool applyUrbanLevel;

    int activeFarmLevel;
    bool applyFarmLevel;

    int activePlantLevel;
    bool applyPlantLevel;

    int activeSpecialIndex;
    bool applySpecialIndex;

    OptionalToggle riverMode, roadMode, walledMode;

    int brushSize;

    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    public UnitType meleeSoldier;
    public UnitType meleeTank;
    public UnitType spearSoldier;
    public UnitType archer;
    public UnitType gunner;
    bool spawnEnemy;

    void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");
        SetEditMode(false);
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                spawnEnemy = !spawnEnemy;
                print(spawnEnemy);
            }
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit(0, spawnEnemy);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit(1, spawnEnemy);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit(2, spawnEnemy);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit(3, spawnEnemy);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit(4, spawnEnemy);
                return;
            }
        }

        previousCell = null;
    }

    void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();
        if(currentCell)
        { 
            if (previousCell && previousCell != currentCell)
                ValidateDrag(currentCell);
            else
                isDrag = false;

            EditCells(currentCell);
            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    HexCell GetCellUnderCursor()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        }
        return null;
    }

    void CreateUnit(int typeID, bool isEnemy)
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            HexUnit u = Instantiate(HexUnit.unitPrefab);
            u.Initialize(typeID, cell, isEnemy);
            hexGrid.AddUnit(u, cell, Random.Range(0f, 360f));
        }
    }
    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
        {
            if(previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell)
    {
        if (cell)
        {
            if (activeTerrainTypeIndex > 0)
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            if(applyElevation)
                cell.Elevation = activeElevation;
            if (applyWaterLevel)
                cell.WaterLevel = activeWaterLevel;
            if (applyUrbanLevel)
                cell.UrbanLevel = activeUrbanLevel;
            if (applyFarmLevel)
                cell.FarmLevel = activeFarmLevel;
            if (applyPlantLevel)
                cell.PlantLevel = activePlantLevel;
            if (applySpecialIndex)
                cell.SpecialIndex = activeSpecialIndex;
            if (riverMode == OptionalToggle.No)
                cell.RemoveRiver();
            if (roadMode == OptionalToggle.No)
                cell.RemoveRoad();
            if (walledMode != OptionalToggle.Ignore)
                cell.Walled = walledMode == OptionalToggle.Yes;
            if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                        otherCell.SetOutgoingRiver(dragDirection);
                    if (roadMode == OptionalToggle.Yes)
                        otherCell.AddRoad(dragDirection);
                }
            }
        }
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }
    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }
    public void SetWallMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }
    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }

    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }
    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int)level;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }
    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }
    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }

    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }
    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }

    public void ShowGrid(bool visible)
    {
        if (visible)
            terrainMaterial.EnableKeyword("GRID_ON");
        else
            terrainMaterial.DisableKeyword("GRID_ON");
    }
    public void SetEditMode(bool toggle)
    {
        enabled = toggle;
    }
}

public enum OptionalToggle
{
    Ignore,
    Yes,
    No
};