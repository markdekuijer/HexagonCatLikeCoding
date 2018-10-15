using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;
    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;
    List<HexCell> pathToTravel;

    public bool hasMovedThisTurn;
    public bool hasAttackThisTurn;

    public HexUnitAnimator animHandler;

    public bool IsTraveling
    {
        get
        {
            return isTraveling;
        }
    }
    bool isTraveling;

    HexCell location, currentTravelLocation;
    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            if (location)
            {
                Grid.DecreaseVisibility(location, VisionRange);
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(value, VisionRange);
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

    public HexGrid Grid { get; set; }

    public int Speed
    {
        get
        {
            return 24;
        }
    }
    public int VisionRange
    {
        get
        {
            return 3;
        }
    }
    public int attackRange;

    public void ValidatePosition()
    {
        transform.localPosition = location.Position;
    }
    public bool IsValidDestination(HexCell cell)
    {
        return cell.IsExplored && !cell.IsUnderWater && !cell.Unit;
    }

    private void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
            if (currentTravelLocation)
            {
                Grid.IncreaseVisibility(location, VisionRange);
                Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                currentTravelLocation = null;
            }
        }
    }

    public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direciton)
    {
        HexEdgeType edgeType = fromCell.GetEdgeType(direciton);
        if(edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }
        int moveCost;
        if (fromCell.HasRoadThroughEdge(direciton))
        {
            moveCost = 1;
        }
        else if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.PlantLevel + toCell.FarmLevel;
        }
        return moveCost;
    }
    public void Travel(List<HexCell> path)
    {

        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }
    IEnumerator TravelPath()
    {
        isTraveling = true;
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        animHandler.SetWalking(isTraveling);
        Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], VisionRange);

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

            for (; t < 1; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
            t -= 1f;
        }
        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, VisionRange);

        for (; t < 1; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d); yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;
        ListPool<HexCell>.Add(pathToTravel);

        pathToTravel = null;
        isTraveling = false;
        hasMovedThisTurn = true;
        animHandler.SetWalking(isTraveling);
    }
    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);
        if(angle > 0f)
        {
            float speed = rotationSpeed / angle;

            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    public void Die()
    {
        if (location) //TODO remove this to keep location
        {
            Grid.DecreaseVisibility(location, VisionRange);
        }
        location.Unit = null;
        animHandler.Die();
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
