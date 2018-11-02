using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitType", menuName = "NewUnit/MakeUnit", order = 1)]
public class UnitType : ScriptableObject
{
    public string objectName = "New Unit Type";
    public int damage;
    public int Health;
    public int speed;
    public int VisionRange;
    public int attackRange;
}
