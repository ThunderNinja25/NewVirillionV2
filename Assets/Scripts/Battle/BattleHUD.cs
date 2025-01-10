using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] HPBar hpBar;

    public void SetData(Creature creature)
    {
        nameText.text = creature.Base.Name;
        levelText.text = "Lvl " + creature.Level;
        hpBar.SetHP((float)creature.HP / creature.MaxHp);
    }
}
