using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color slpColor;
    [SerializeField] Color parColor;
    [SerializeField] Color frzColor;

    Creature _creature;
    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Creature creature)
    {
        _creature = creature;

        nameText.text = creature.Base.Name;
        levelText.text = "Lvl " + creature.Level;
        hpBar.SetHP((float)creature.HP / creature.MaxHp);
        SetExp();

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor },
            {ConditionID.brn, brnColor },
            {ConditionID.slp, slpColor },
            {ConditionID.par, parColor },
            {ConditionID.frz, frzColor }
        };

        SetStatusText();
        _creature.OnStatusChanged += SetStatusText;
    }

    void SetStatusText()
    {
        if(_creature.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _creature.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_creature.Status.Id];
        }
    }

    public void SetExp()
    {
        if (expBar == null)
        {
            return;
        }

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3 (normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth()
    {
        if (expBar == null)
        {
            yield break;
        }

        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    float GetNormalizedExp()
    {
        int currLevelExp = _creature.Base.GetExpForLevel(_creature.Level);
        int nextLevelExp = _creature.Base.GetExpForLevel(_creature.Level + 1);

        float normalizedExp = (float)(_creature.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public IEnumerator UpdateHP()
    {
        if(_creature.HpChanged)
        {
            yield return hpBar.SetHPSmooth((float)_creature.HP / _creature.MaxHp);
            _creature.HpChanged = false;
        }
    }
}
