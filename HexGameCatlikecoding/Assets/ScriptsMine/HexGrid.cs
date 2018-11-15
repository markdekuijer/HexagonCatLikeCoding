using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class HexGrid : MonoBehaviour
{
    public int seed;
    public HexGridChunk chunkPrefab;
    HexGridChunk[] chunks;

    public int cellCountX = 20, cellCountZ = 15;
    int chunkCountX, chunkCountZ;

    public HexCell cellPrefab;
    public Texture2D noiseSource;

    public Text cellLabelPrefab;
    HexCell[] cells;

    public List<HexUnit> units = new List<HexUnit>();

    HexCellPriorityQueue searchOpenNodes;
    int searchOpenNodesPhase;

    HexCell currentPathFrom, currentPathTo;
    bool currentPathExists;
    public bool HasPath
    {
        get
        {
            return currentPathExists;
        }
    }

    HexCellShaderData cellShaderData;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;
        CreateMap(cellCountX, cellCountZ);
    }

    void Start()
    {
        TurnbasedManager.Instance.grid = this;
    }

    void OnEnable()
    {
        if(!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            ResetVisibility();
        }
    }

    public void CreateAllyAndEnemyList()
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (!units[i].isEnemy)
                TurnbasedManager.Instance.allyUnits.Add(units[i]);
            else
                TurnbasedManager.Instance.enemyUnits.Add(units[i]);
        }
    }

    public bool CreateMap(int x, int z)
    {
        if(x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            print("Unsupported map size.");
            return false;
        }

        ClearPath();
        ClearUnits();
        if (chunks != null)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                Destroy(chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].DisableHighlight();
        }

        return true;
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }
    public HexCell GetCell(HexCoordinates coords)
    {
        int z = coords.Z;
        if (z < 0 || z >= cellCountZ)
            return null;

        int x = coords.X + z / 2;
        if (x < 0 || x >= cellCountX)
            return null;

        return cells[x + z * cellCountX];
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }
    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;

        cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }


        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);
        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }
    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit, List<HexCell> cellsToHighlight)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.unitType.speed, cellsToHighlight);
        sw.Stop();
        //print(sw.ElapsedMilliseconds);
    }
    public bool Search(HexCell fromCell, HexCell toCell, HexUnit unit, bool isEnemy = false)
    {
        int speed = unit.unitType.speed;
        searchOpenNodesPhase += 2;

        if (searchOpenNodes == null)
            searchOpenNodes = new HexCellPriorityQueue();
        else
            searchOpenNodes.Clear();

        fromCell.SearchPhase = searchOpenNodesPhase;
        fromCell.Distance = 0;
        searchOpenNodes.Enqueue(fromCell);

        while (searchOpenNodes.Count > 0)
        {
            HexCell current = searchOpenNodes.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchOpenNodesPhase)
                    continue;

                if (!isEnemy)
                {
                    if (!unit.IsValidDestination(neighbor))
                    {
                        continue;
                    }
                }
                else
                {
                    if (neighbor.Unit && neighbor != toCell)
                        continue;
                }

                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                    continue;

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;

                if (turn > currentTurn)
                    distance = turn * speed + moveCost;

                if (neighbor.SearchPhase < searchOpenNodesPhase)
                {
                    neighbor.SearchPhase = searchOpenNodesPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchOpenNodes.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchOpenNodes.Change(neighbor, oldPriority);
                }
            }

        }
        return false;
    }

    [HideInInspector] public List<HexCell> attackableCells = new List<HexCell>();
    [HideInInspector] public List<HexCell> bonusChecks = new List<HexCell>();
    public List<HexCell> SearchMovementArea(HexCell fromCell, int steps)
    {
        attackableCells.Clear();
        int attackRange = fromCell.Unit.unitType.attackRange;
        List<HexCell> bonusChecks = new List<HexCell>();

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Distance = int.MaxValue;
            cells[i].DisableHighlight();
        }
        fromCell.EnableHighlight(Color.blue);

        List<HexCell> frontier = new List<HexCell>();
        List<HexCell> cellsToHighlight = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);
        cellsToHighlight.Add(fromCell);
        while (frontier.Count > 0)
        {
            HexCell current = frontier[0];
            frontier.RemoveAt(0);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }
                if (neighbor.IsUnderWater)
                {
                    continue;
                }
                if (!neighbor.Explorable || !neighbor.IsExplored)
                {
                    continue;
                }
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }
                if (neighbor.Unit)
                {
                    if (neighbor.Unit.isEnemy)
                        attackableCells.Add(neighbor);

                    continue;
                }
                int distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                {
                    distance += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel +
                        neighbor.PlantLevel;
                }
                if(distance > steps)
                {
                    if(!bonusChecks.Contains(current))
                        bonusChecks.Add(current);
                    continue;
                }
                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    if (neighbor.Explorable)
                    {
                        neighbor.EnableHighlight(Color.white);
                        cellsToHighlight.Add(neighbor);
                    }
                    frontier.Add(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                }
                frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }
        for (int i = 0; i < bonusChecks.Count; i++)
        {
            searchExtendingAttackArea(bonusChecks[i], attackRange);
        }

        return cellsToHighlight;
    }
    public List<HexCell> searchAttackArea(HexCell fromCell, int range)
    {
        if (range == 0)
            return new List<HexCell>();

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Distance = int.MaxValue;
            cells[i].DisableHighlight();
        }

        List<HexCell> frontier = new List<HexCell>();
        List<HexCell> cellsToHighlight = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);
        cellsToHighlight.Add(fromCell);
        while (frontier.Count > 0)
        {
            HexCell current = frontier[0];
            frontier.RemoveAt(0);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }
                if (neighbor.IsUnderWater)
                {
                    continue;
                }
                if (!neighbor.Explorable || !neighbor.IsExplored)
                {
                    continue;
                }
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }
                int distance = current.Distance;
                if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += 1;
                }
                if (distance > range)
                {
                    continue;
                }
                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    if (neighbor.Explorable)
                    {
                        if(neighbor.Unit)
                            if (neighbor.Unit.isEnemy)
                            {
                                neighbor.EnableHighlight(Color.red);
                                cellsToHighlight.Add(neighbor);
                            }
                    }
                    frontier.Add(neighbor);
                }
                frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }
        return cellsToHighlight;
    }
    public void searchExtendingAttackArea(HexCell fromCell, int range)
    {
        if (range == 0)
            return;

        List<HexCell> frontier = new List<HexCell>();
        List<HexCell> cellsToHighlight = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);
        cellsToHighlight.Add(fromCell);
        while (frontier.Count > 0)
        {
            HexCell current = frontier[0];
            frontier.RemoveAt(0);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }
                if (neighbor.IsUnderWater)
                {
                    continue;
                }
                if (!neighbor.Explorable || !neighbor.IsExplored)
                {
                    continue;
                }
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }
                int distance = current.Distance;
                if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += 1;
                }
                if (distance > range)
                {
                    continue;
                }
                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    if (neighbor.Explorable)
                    {
                        cellsToHighlight.Add(neighbor);
                    }
                    frontier.Add(neighbor);
                }
                frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }
        for (int i = 0; i < cellsToHighlight.Count; i++)
        {
            if(cellsToHighlight[i] != fromCell)
            {
                if(cellsToHighlight[i].Unit)
                    if (cellsToHighlight[i].Unit.isEnemy)
                    {
                        attackableCells.Add(cellsToHighlight[i]);
                        cellsToHighlight[i].EnableHighlight(Color.red);
                    }

            }
        }
    }

    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchOpenNodesPhase += 2;

        if (searchOpenNodes == null)
            searchOpenNodes = new HexCellPriorityQueue();
        else
            searchOpenNodes.Clear();

        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchOpenNodesPhase;
        fromCell.Distance = 0;
        searchOpenNodes.Enqueue(fromCell);

        HexCoordinates fromCoordinates = fromCell.coordinates;
        while (searchOpenNodes.Count > 0)
        {
            HexCell current = searchOpenNodes.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchOpenNodesPhase || !neighbor.Explorable)
                    continue;

                int distance = current.Distance + 1;
                if (distance + neighbor.ViewElevation > range || distance > fromCoordinates.DistanceTo(neighbor.coordinates))
                    continue;

                if (neighbor.SearchPhase < searchOpenNodesPhase)
                {
                    neighbor.SearchPhase = searchOpenNodesPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchOpenNodes.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchOpenNodes.Change(neighbor, oldPriority);
                }
            }

        }
        return visibleCells;
    }
    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
    public void ResetVisibility()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].ResetVisibility();
        }
        for (int i = 0; i < units.Count; i++)
        {
            IncreaseVisibility(units[i].Location, units[i].unitType.VisionRange);
        }
    }

    public List<HexCell> GetPath(int speed)
    {
        if (!currentPathExists)
            return null;

        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            if(c.Distance < speed)
                path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }
    public List<HexCell> GetPathWithoutExistCheck(int speed, HexCell to, HexCell from)
    {
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = to; c != from; c = c.PathFrom)
        {
            if (c.Distance < speed)
                path.Add(c);
        }
        path.Add(from);
        path.Reverse();
        return path;
    }
    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        currentPathFrom = currentPathTo = null;
    }
    void ShowPath(int speed, List<HexCell> cellsToHighlight)
    {
        if (currentPathExists)
        {
            for (int i = 0; i < cellsToHighlight.Count; i++)
            {
                cellsToHighlight[i].EnableHighlight(Color.white);
            }

            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                if (turn < 1)
                    current.EnableHighlight(Color.grey);
                current = current.PathFrom;
            }
        }
        else
        {
            for (int i = 0; i < cellsToHighlight.Count; i++)
            {
                cellsToHighlight[i].EnableHighlight(Color.white);
            }

        }
        currentPathFrom.EnableHighlight(Color.blue);
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;   
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }
    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }
    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }
    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        if(x != cellCountX || z != cellCountZ)
            if (!CreateMap(x, z))
                return;

        bool originalImmediateMode = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader, header);
        }

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }

        if(header >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
        CreateAllyAndEnemyList();
    }
}

public static class ListPool<T>
{
    static Stack<List<T>> stack = new Stack<List<T>>();

    public static List<T> Get()
    {
        if (stack.Count > 0)
        {
            return stack.Pop();
        }
        return new List<T>();
    }

    public static void Add(List<T> list)
    {
        list.Clear();
        stack.Push(list);
    }
}