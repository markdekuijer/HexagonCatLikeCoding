﻿using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;

    [SerializeField] HexCell[] neighbors;

    public RectTransform uiRect;

    public HexGridChunk chunk;
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    int elevation = int.MinValue;
    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
                return;
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f)
                * HexMetrics.elevationPerturbStrenght;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;

            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if(roads[i] && GetElevatoinDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    Color color;
    public Color Color
    {
        get
        {
            return color;
        }
        set
        {
            if (color == value)
                return;
            color = value;
            Refresh();
        }
    }

    public float StreamBedY
    {
        get
        {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }
    public float RiverSurfaceY
    {
        get
        {
            return (elevation + HexMetrics.waterElevationOffset ) * HexMetrics.elevationStep;
        }
    }
    public float WaterSurfaceY
    {
        get
        {
            return (WaterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    bool hasIncomingRiver, hasOutgoingRiver;
    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
    }
    public bool HasOutgoingRiver
    {
        get
        {
            return hasOutgoingRiver;
        }
    }
    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }
    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    HexDirection incomingRiver, outgoingRiver;
    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
    }
    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }
    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    [SerializeField] bool[] roads;
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] == true)
                    return true;
            }
            return false;
        }
    }

    int waterLevel;
    public int WaterLevel
    {
        get
        {
            return  waterLevel;
        }
        set
        {
            if (waterLevel == value)
                return;

            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    public bool IsUnderWater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    #region rivers
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
            return;

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
            RemoveIncomingRiver();

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
            return;
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
            return;
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor &&
        (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    void ValidateRivers()
    {
        if (hasOutgoingRiver &&!IsValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver &&!GetNeighbor(incomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }
    #endregion

    #region Roads
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction)
            && GetElevatoinDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoad()
    {
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    public void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public int GetElevatoinDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    #endregion

    #region neighborsAndEdge
    public HexCell GetNeighbor (HexDirection direction)
    {
        return neighbors[(int)direction];
    }
    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }
    #endregion

    void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }
}