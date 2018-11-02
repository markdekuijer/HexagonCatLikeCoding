using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;
    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;
    List<HexCell> pathToTravel;

    public bool hasMovedThisTurn;
    public bool hasAttackThisTurn;
    public bool hasTurned;
    public bool isEnemy;

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
                if (!isEnemy)
                {
                    Grid.DecreaseVisibility(location, VisionRange);
                }
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            if(!isEnemy)
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

    public int damage;
    public int Health;
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
                if (!isEnemy)
                {
                    Grid.IncreaseVisibility(location, VisionRange);
                    Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                }
                currentTravelLocation = null;
            }
        }
    }

    private void LateUpdate()
    {
        if(isEnemy && location != null)
            location.EnableHighlight(Color.red);
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
    public void CalculateNextMove(HexGrid grid, List<HexUnit> unitsToCheck)
    {
        //da wae
        attackRange = 3;
        List<HexUnit> allUnits = new List<HexUnit>(unitsToCheck);
        if (allUnits.Count == 0)
        {
            print("These bitches empty, Yeet");
            hasTurned = true;
            return;
        }

        HexUnit target = TurnbasedManager.Instance.GetClosestAlly(location.coordinates, allUnits);
        bool canWalkThisPath = grid.Search(location, target.location, this, true);
        if (canWalkThisPath)
        {
            List<HexCell> path = grid.GetPathWithoutExistCheck(Speed, target.location, location);
            int reduceSteps = attackRange - 1;
            if(path.Count > 1)
            {
                if (path[path.Count - 1].Unit)
                {
                    path.RemoveAt(path.Count - 1);

                    if (reduceSteps > 0)
                    {
                        if(path[path.Count - 1].coordinates.DistanceTo(target.location.coordinates) != 1)
                        {
                            for (int i = 0; i < reduceSteps; i++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }
                        }
                    }
                }
                if(path.Count > 1)
                {
                    if(path[path.Count - 1].coordinates.DistanceTo(target.location.coordinates) == 1)
                    {
                        for (int i = 0; i < reduceSteps; i++)
                        {
                            path.RemoveAt(path.Count - 1);
                        }
                    }
                }

                Travel(path, null, target.location, false);
            }
        }
        else
        {
            if (!target)
            {
                Debug.LogError("fcked up no target existing");
            }
            allUnits.Remove(target);
            CalculateNextMove(grid, allUnits);
        }
    }

    public void Travel(List<HexCell> path, HexGameUI gameUI, HexCell c, bool isPlayer)
    {
        HexCell origin = location;
        if(path.Count != 0)
        {
            location.Unit = null;
            location = path[path.Count - 1];
        }
        location.Unit = this;
        pathToTravel = path;
        StopAllCoroutines();
        if (isPlayer)
            StartCoroutine(TravelPathPlayer(gameUI, c));
        else
            StartCoroutine(TravelPathEnemy(c, origin));
    }
    IEnumerator TravelPathPlayer(HexGameUI gameUI, HexCell attackCell)
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
        if(gameUI)
            gameUI.AttackAfterCheck(attackCell);
    }
    IEnumerator TravelPathEnemy(HexCell attackCell, HexCell originalCell)
    {
        //TODO Attack if in reach
        if (originalCell.coordinates.DistanceTo(attackCell.coordinates) <= attackRange)
        {
            InitAttack(attackCell, null); //TODO let this start after travel
            yield break;
        }
        isTraveling = true;
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        animHandler.SetWalking(isTraveling);
        //Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], VisionRange); //REMOVE

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * 0.5f;
            //Grid.IncreaseVisibility(pathToTravel[i], VisionRange); //REMOVE

            for (; t < 1; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            //Grid.DecreaseVisibility(pathToTravel[i], VisionRange); //REMOVE
            t -= 1f;
        }
        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;
        //Grid.IncreaseVisibility(location, VisionRange); //REMOVE

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
        //if (gameUI)
        //    gameUI.AttackAfterCheck(attackCell);
        yield return null;
        if(location.coordinates.DistanceTo(attackCell.coordinates) <= attackRange)
            InitAttack(attackCell, null); 
        else
            hasTurned = true;
    }

    #region attack
    public void InitAttack(HexCell cell, HexGameUI gameUI)
    {
        StartCoroutine(Attack(cell, gameUI));
    }
    IEnumerator Attack(HexCell attackedCell, HexGameUI gameUI)
    {
        yield return LookAt(attackedCell.Position);
        animHandler.InitAttack();
        hasAttackThisTurn = true;
        if(gameUI)
            gameUI.CloseSelect();
        DoDamage(attackedCell.Unit);
        yield return new WaitForSeconds(2f);
        hasTurned = true;
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
    #endregion

    #region dmg
    public void DoDamage(HexUnit otherUnit)
    {
        otherUnit.TakeDamage(damage); 
    }
    public void TakeDamage(int damage)
    {
        Health -= damage;
        if(Health <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        if (location) //TODO remove this to keep location
        {
            if (!isEnemy)
            {
                Grid.DecreaseVisibility(location, VisionRange);
            }
        }
        location.Unit = null;
        animHandler.Die();

        if (isEnemy)
            TurnbasedManager.Instance.enemyUnits.Remove(this);
        else
            TurnbasedManager.Instance.allyUnits.Remove(this);
        //Grid.RemoveUnit(this);
    }
    #endregion

    #region savedata
    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
        writer.Write(isEnemy);
    }
    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        bool isEnemy = reader.ReadBoolean();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation, isEnemy);
    }
    #endregion
}
