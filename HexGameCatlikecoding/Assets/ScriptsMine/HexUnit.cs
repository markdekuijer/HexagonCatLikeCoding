using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class HexUnit : MonoBehaviour
{
    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;
    List<HexCell> pathToTravel;

    public bool hasMovedThisTurn;
    public bool hasAttackThisTurn;
    public bool hasTurned;
    public bool isEnemy;

    public HexUnitAnimator animHandler;
    private List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();
    private List<MeshRenderer> renderers = new List<MeshRenderer>();

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
                    Grid.DecreaseVisibility(location, unitType.VisionRange);
                }
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            if(!isEnemy)
                Grid.IncreaseVisibility(value, unitType.VisionRange);
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

    public UnitType unitType;
    public int typeID;
    int health;
    public int Health
    {
        get
        {
            return health;
        }
        private set
        {
            if (health != value)
                health = value;
        }
    }


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
                    Grid.IncreaseVisibility(location, unitType.VisionRange);
                    Grid.DecreaseVisibility(currentTravelLocation, unitType.VisionRange);
                }
                currentTravelLocation = null;
            }
        }
    }
    public void Initialize(int id, HexCell spawnCell, bool isEnemy)
    {
        this.isEnemy = isEnemy;
        typeID = id;
        health = unitType.Health;

        skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        renderers = GetComponentsInChildren<MeshRenderer>().ToList();
        if(!spawnCell.IsExplored || !spawnCell.IsVisible)
        {
            if (isEnemy)
                DisplayRenderers(true);
        }
    }

    public void DisplayRenderers(bool show)
    {
        for (int i = 0; i < skinnedRenderers.Count; i++)
        {
            skinnedRenderers[i].material.color = show ? new Color(1,1,1,1) : new Color(0,0,0,0);
        }
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].material.color = show ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 0);
        }
    }
    public IEnumerator DisplayRenderers(float aValue, float time)
    {
        float alpha = skinnedRenderers[0].material.color.a;
        for (float t = 0; t < 1; t += Time.deltaTime / time)
        {
            for (int i = 0; i < skinnedRenderers.Count; i++)
            {
                Color newColor = new Color(1, 1, 1, Mathf.Lerp(alpha, aValue, t));
                skinnedRenderers[i].material.color = newColor;
            }
            for (int i = 0; i < renderers.Count; i++)
            {
                Color newColor = new Color(1, 1, 1, Mathf.Lerp(alpha, aValue, t));
                renderers[i].material.color = newColor;
            }

            yield return null;
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
    public void CalculateNextMove(HexGrid grid, List<HexUnit> unitsToCheck)
    {
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
            List<HexCell> path = grid.GetPathWithoutExistCheck(unitType.speed, target.location, location);
            int reduceSteps = unitType.attackRange - 1;
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
        Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], unitType.VisionRange);

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], unitType.VisionRange);

            for (; t < 1; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], unitType.VisionRange);
            t -= 1f;
        }
        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, unitType.VisionRange);

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
        if (originalCell.coordinates.DistanceTo(attackCell.coordinates) <= unitType.attackRange)
        {
            InitAttack(attackCell, null); 
            yield break;
        }
        isTraveling = true;
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        animHandler.SetWalking(isTraveling);

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * 0.5f;

            for (; t < 1; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
            if (currentTravelLocation.IsExplored && currentTravelLocation.IsVisible)
                DisplayRenderers(true);
        }
        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;

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
        yield return null;
        if(location.coordinates.DistanceTo(attackCell.coordinates) <= unitType.attackRange)
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
        Grid.IncreaseVisibility(location, unitType.attackRange);
        animHandler.InitAttack();
        hasMovedThisTurn = true;
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
        otherUnit.TakeDamage(unitType.damage); 
    }
    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        if(unitType.objectName == "castle")
        {
            //TODO start gameover system;
            print("GAME OVER");
            return;
        }

        if (location) 
        {
            if (!isEnemy)
            {
                Grid.DecreaseVisibility(location, unitType.VisionRange);
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
        writer.Write(typeID);
    }
    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        bool isEnemy = reader.ReadBoolean();
        int typeID = reader.ReadInt32();
        HexUnit u = Instantiate(HexGameUI.instance.unitTypes.unitTypeIDs[typeID].GetComponent<HexUnit>());
        u.Initialize(typeID, grid.GetCell(coordinates), isEnemy);
        grid.AddUnit(u, grid.GetCell(coordinates), orientation);
    }
    #endregion
}

