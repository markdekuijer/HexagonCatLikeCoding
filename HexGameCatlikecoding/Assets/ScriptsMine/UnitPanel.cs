using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitPanel : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text rangeText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text visionText;

    [SerializeField] private Slider slider;

    public void StartDisplay(HexUnit u)
    {
        print("START DISPLAY");
        nameText.text = u.unitType.objectName;
        healthText.text = u.Health.ToString();
        attackText.text = u.unitType.damage.ToString();
        rangeText.text = u.unitType.attackRange.ToString();
        speedText.text = u.unitType.speed.ToString();
        visionText.text = u.unitType.VisionRange.ToString();

        slider.value = (u.Health / u.unitType.Health);
    }
}
