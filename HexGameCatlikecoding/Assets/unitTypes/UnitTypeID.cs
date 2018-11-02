using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TypeIDs", menuName = "NewUnit/MakeList", order = 1)]
public class UnitTypeID : ScriptableObject
{
    public List<UnitType> unitTypeIDs = new List<UnitType>();
}
