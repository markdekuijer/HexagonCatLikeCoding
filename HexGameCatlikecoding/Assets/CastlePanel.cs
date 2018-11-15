using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CastlePanel : MonoBehaviour
{
    public Text healthText;
    public Slider healthSlider;

    public void SetPanel(int maxHP, int currentHP)
    {
        healthText.text = "HP " + currentHP.ToString();
        healthSlider.value = currentHP / maxHP;
    }
}
