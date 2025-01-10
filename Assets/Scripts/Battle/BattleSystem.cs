using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleHUD playerHUD;
    [SerializeField] BattleHUD enemyHUD;

    private void Start()
    {
        SetupBattle();
    }

    public void SetupBattle()
    {
        playerUnit.Setup();
        enemyUnit.Setup();
        playerHUD.SetData(playerUnit.Creature);
        enemyHUD.SetData(enemyUnit.Creature);
    }
}
