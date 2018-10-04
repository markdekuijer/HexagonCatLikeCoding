using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    public HexUnit unitPrefab;
    List<HexUnit> units = new List<HexUnit>();

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
        HexUnit.unitPrefab = unitPrefab;

        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        CreateMap(cellCountX, cellCountZ);
    }

    void OnEnable()
    {
        if(!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
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

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);
        sw.Stop();
        print(sw.ElapsedMilliseconds);
    }
    bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        searchOpenNodesPhase += 2;

        if (searchOpenNodes == null)
            searchOpenNodes = new HexCellPriorityQueue();
        else
            searchOpenNodes.Clear();

        fromCell.SearchPhase = searchOpenNodesPhase;
        fromCell.Distance = 0;
        searchOpenNodes.Enqueue(fromCell);

        while(searchOpenNodes.Count > 0)
        {
            HexCell current = searchOpenNodes.Dequeue();
            current.SearchPhase += 1;

            if(current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchOpenNodesPhase) 
                    continue;
                if (neighbor.IsUnderWater || neighbor.Unit)
                    continue;

                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                    continue;

                int moveCost = 0;
                if (current.HasRoadThroughEdge(d))
                {
                    moveCost += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    moveCost += edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

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
                else if(distance < neighbor.Distance)
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

    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

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
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchOpenNodesPhase)
                    continue;

                int distance = current.Distance + 1;
                if (distance > range)
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

    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
            return null;

        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
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
    void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
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

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
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