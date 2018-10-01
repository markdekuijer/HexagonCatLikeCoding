using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class HexMetrics
{
    public const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1f / OuterToInner;

    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * 0.866025404f;

    public const float solidFactor = 0.8f;
    public const float waterFactor = 0.6f;
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 3;

    public const int terracesPerSlope = 2;
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    public static Texture2D noiseSource;
    public const float cellPerturbStrengt = 4f; //4f; original from tutorial
    public const float noiseScale = 0.003f;
    public const float elevationPerturbStrenght = 1.5f;

    public const int chunkSizeX = 5, chunkSizeZ = 5;

    public const float streamBedElevationOffset = -1.75f;
    public const float waterElevationOffset = -0.5f;
    public const float waterBlendFactor = 1f - waterFactor;

    public const int hashGridSize = 256;
    public const float hashGridScale = 0.25f;
    static HexHash[] hashGrid;

    public const float wallHeight = 4f;
	public const float wallThickness = 0.75f;
    public const float wallElevationOffset = verticalTerraceStepSize;
    public const float wallTowerThreshold = 0.5f;
    public const float wallYOffset = -1f;

    public const float bridgeDesignlenght = 7f;

    static float[][] featureThreshold =
    {
        new float[] {0.0f, 0.0f ,0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius)
    };

    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }

    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
            x += hashGridSize;

        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
            z += hashGridSize;

        return hashGrid[x + z * hashGridSize];
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v =
            near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThreshold[level - 1];
    }

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }
    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }
    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }
    public static Vector3 GetSolidMiddleEdge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * 0.5f * solidFactor;
    }

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * waterFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
    }
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }

        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }

        return HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale, 
            position.z * noiseScale);
    }
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2 - 1) * cellPerturbStrengt;
        position.z += (sample.z * 2 - 1) * cellPerturbStrengt;
        return position;
    }

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0;
        offset.z = far.z - near.z;

        return offset.normalized * (wallThickness * 0.5f);
    }
}

[System.Serializable]
public struct HexCoordinates
{
    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }
    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;

        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }


        return new HexCoordinates(iX, iZ);
    }

    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    } 

    public int DistanceTo(HexCoordinates other)
    {
        return
            ((x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z)) / 2;
    }

    [SerializeField]
    private int x, z;

    public int X
    {
        get
        {
            return x;
        }
    }
    public int Y
    {
        get
        {
            return -X - Z;
        }
    }
    public int Z
    {
        get
        {
            return z;
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }
    public void Load()
    {

    }

    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }
    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}

public class HexCellPriorityQueue
{
    List<HexCell> list = new List<HexCell>();
    int minimum = int.MaxValue;

    int count = 0;
    public int Count
    {
        get
        {
            return count;
        }
    }


    public void Enqueue(HexCell cell)
    {
        count += 1;
        int priority = cell.SearchPriority;
        if (priority < minimum)
            minimum = priority;

        while (priority >= list.Count)
            list.Add(null);

        cell.NextWithSamePriority = list[priority];
        list[priority] = cell;
    }

    public HexCell Dequeue()
    {
        count -= 1;
        for (; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];
            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null;
    }

    public void Change(HexCell cell, int oldPriority)
    {
        HexCell current = list[oldPriority];
        HexCell next = current.NextWithSamePriority;
        if (current == null)
        {
            list[oldPriority] = next;
        }
        else
        {
            while(next != current)
            {
                current = next;
                if(next == null)
                {
                    //print("a");
                }
                next = current.NextWithSamePriority;
            }
            current.NextWithSamePriority = cell.NextWithSamePriority;
            Enqueue(cell);
            count -= 1;
        }
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}

[System.Serializable]
public struct HexFeatureCollection
{
    public Transform[] prefabs;

    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}

public struct HexHash
{
    public float a, b, c, d, e;

    public static HexHash Create()
    {
        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }
}