using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;


public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, BattleOver}
public enum BattleAction { Move, SwitchCreature, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogueBox dialogueBox;
    [SerializeField] PartyScreen partyScreen;

    public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;

    CreatureParty playerParty;
    Creature wildCreature;

    public void StartBattle(CreatureParty playerParty, Creature wildCreature)
    {
        this.playerParty = playerParty;
        this.wildCreature = wildCreature;
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Setup(playerParty.GetHealthyCreature());
        enemyUnit.Setup(wildCreature);

        partyScreen.Init();

        dialogueBox.SetMoveNames(playerUnit.Creature.Moves);

        yield return dialogueBox.TypeDialogue($"A Wild {enemyUnit.Creature.Base.Name} appeared.");

        ActionSelection();
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Creatures.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogueBox.SetDialogue("Choose an action");
        dialogueBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Creatures);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogueBox.EnableActionSelector(false);
        dialogueBox.EnableDialogueText(false);
        dialogueBox.EnableMoveSelector(true);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Creature.CurrentMove = playerUnit.Creature.Moves[currentMove];
            enemyUnit.Creature.CurrentMove = enemyUnit.Creature.GetRandomMove();

            int playerMovePriority = playerUnit.Creature.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Creature.CurrentMove.Base.Priority;

            //Check who goes first
            bool playerGoesFirst = true;
            if(enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if(enemyMovePriority == playerMovePriority)
                playerGoesFirst = playerUnit.Creature.Speed >= enemyUnit.Creature.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondCreature = secondUnit.Creature;

            //First Turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Creature.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;
            
            if(secondCreature.HP > 0)
            {
                //Second Turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Creature.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            if(playerAction == BattleAction.SwitchCreature)
            {
                var selectedCreature = playerParty.Creatures[currentMember];
                state = BattleState.Busy;
                yield return SwitchCreature(selectedCreature);
            }

            //Enemy Turn
            var enemyMove = enemyUnit.Creature.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Creature.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Creature);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Creature);

        move.PP--;
        yield return dialogueBox.TypeDialogue($"{sourceUnit.Creature.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Creature, targetUnit.Creature))
        {
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Creature, targetUnit.Creature, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Creature.TakeDamage(move, sourceUnit.Creature);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Creature.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Creature, targetUnit.Creature, secondary.Target);
                }
            }

            if (targetUnit.Creature.HP <= 0)
            {
                yield return dialogueBox.TypeDialogue($"{targetUnit.Creature.Base.Name} Fainted");
                targetUnit.PlayFaintAnimation();
                yield return new WaitForSeconds(2f);

                CheckForBattleOver(targetUnit);
            }
        }

        else
        {
            yield return dialogueBox.TypeDialogue($"{sourceUnit.Creature.Base.Name}'s attack missed");
        }


    }

    IEnumerator RunMoveEffects(MoveEffects effects, Creature source, Creature target, MoveTarget moveTarget)
    {
        //Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                target.ApplyBoosts(effects.Boosts);
            }
        }

        // Status Conditions
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        // Volatile Status Conditions
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // Burn & Poison Statuses
        sourceUnit.Creature.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Creature);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Creature.HP <= 0)
        {
            yield return dialogueBox.TypeDialogue($"{sourceUnit.Creature.Base.Name} Fainted");
            sourceUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
        }
    }

    bool CheckIfMoveHits(Move move, Creature source, Creature target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Creature creature)
    {
        while(creature.StatusChanges.Count > 0)
        {
            var message = creature.StatusChanges.Dequeue();
            yield return dialogueBox.TypeDialogue(message);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextCreature = playerParty.GetHealthyCreature();
            if (nextCreature != null)
            {
                OpenPartyScreen();
            }
            else
            {
                BattleOver(false);
            }
        }
        else
        {
            BattleOver(true);
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if(damageDetails.Critical > 1f)
        {
            yield return dialogueBox.TypeDialogue("A critical hit!");
        }
        if(damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogueBox.TypeDialogue("It's super effective!");
        }
        if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogueBox.TypeDialogue("It's not very effective!");
        }
    }

    public void HandleUpdate()
    {
        if(state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if(state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if(state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentAction++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentAction--;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown (KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogueBox.UpdateActionSelection(currentAction);

        if(Input.GetKeyDown(KeyCode.Z))
        {
            if(currentAction == 0)
            {
                //Fight
                MoveSelection();
            }
            else if(currentAction == 1)
            {
                //Bag
            }
            else if (currentAction == 2)
            {
                //Creature
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //Run
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentMove++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentMove--;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Creature.Moves.Count - 1);

        dialogueBox.UpdateMoveSelection(currentMove, playerUnit.Creature.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = playerUnit.Creature.Moves[currentMove];
            if (move.PP == 0) return;

            dialogueBox.EnableMoveSelector(false);
            dialogueBox.EnableDialogueText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableMoveSelector(false);
            dialogueBox.EnableDialogueText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentMember++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentMember--;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;

        currentMove = Mathf.Clamp(currentMember, 0, playerParty.Creatures.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedMember = playerParty.Creatures[currentMember];
            if(selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted Creature!");
                return;
            }
            if(selectedMember == playerUnit.Creature)
            {
                partyScreen.SetMessageText("You can't switch with the same Creature!");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if(prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchCreature));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchCreature(selectedMember));
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
        }
    }

    IEnumerator SwitchCreature(Creature newCreature)
    {
        playerUnit.Creature.CureVolatileStatus();
        if (playerUnit.Creature.HP > 0)
        {
            yield return dialogueBox.TypeDialogue($"Come back {playerUnit.Creature.Base.Name}");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }
        playerUnit.Setup(newCreature);

        dialogueBox.SetMoveNames(newCreature.Moves);

        yield return dialogueBox.TypeDialogue($"Go {newCreature.Base.Name}!");
        
        state = BattleState.RunningTurn;
    }
}
