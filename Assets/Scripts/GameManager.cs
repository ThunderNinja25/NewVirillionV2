using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle}
public class GameManager : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera mainCamera;

    GameState state;

    private void Awake()
    {
        ConditionsDB.Init();
    }

    private void Start()
    {
        playerController.OnEncountered += StartBattle;
        battleSystem.OnBattleOver += EndBattle;
    }

    void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<CreatureParty>();
        var wildCreature = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildCreature();

        battleSystem.StartBattle(playerParty, wildCreature);
    }

    void EndBattle(bool won)
    {
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
        }
        else if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
    }
}
