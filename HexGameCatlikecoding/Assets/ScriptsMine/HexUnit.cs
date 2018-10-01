using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;

    HexCell location;
    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            if (location)
                location.Unit = null;
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    float orientation;
    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0, value, 0);
        }
    }

    public void ValidatePosition()
    {
        transform.localPosition = location.Position;
    }
    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderWater && !cell.Unit;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }
    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }
}
