using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollectoins, farmCollections, plantCollections;
    public HexMesh walls;
    Transform container;

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);

        Transform prefab = PickFeature(urbanCollectoins, cell.UrbanLevel, hash.a, hash.d);
        Transform otherPrefab = PickFeature(farmCollections, cell.FarmLevel, hash.b, hash.d);

        float usedHash = hash.a;
        if (prefab)
        {
            if (otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickFeature(plantCollections, cell.PlantLevel, hash.c, hash.d);
        if (prefab)
        {
            if (otherPrefab && hash.c < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
            return;

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);
    }

    Transform PickFeature(HexFeatureCollection[] collections, int level, float hash, float choice)
    {
        if(level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                    return collections[i].Pick(choice);
            }
        }
        return null;
    }

    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell)
    {
        if(nearCell.Walled != farCell.Walled)
        {
            AddWallSegment(near.v1, far.v1, near.v5, far.v5);
        }
    }

    void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
    {
        Vector3 left = Vector3.Lerp(nearLeft, farLeft, 0.5f);
        Vector3 right = Vector3.Lerp(nearRight, farRight, 0.5f);

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left;
        v2 = v4 = right;
        v3.y = v4.y = left.y + HexMetrics.wallHeight;
        walls.AddQuad(v1, v2, v3, v4);
        walls.AddQuad(v2, v1, v4, v3);
    }

    public void Clear()
    {
        if (container)
            Destroy(container.gameObject);
        container = new GameObject("Feature Container").transform;
        container.SetParent(transform, false);
        walls.Clear();
    }

    public void Apply()
    {
        walls.Apply();
    }
}
