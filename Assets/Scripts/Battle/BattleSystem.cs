using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver }
public enum BattleAction { Move, SwitchCreature, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogueBox dialogueBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject ballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;

    public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    CreatureParty playerParty;
    CreatureParty trainerParty;
    Creature wildCreature;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;
    MoveBase moveToLearn;

    public void StartBattle(CreatureParty playerParty, Creature wildCreature)
    {
        this.playerParty = playerParty;
        this.wildCreature = wildCreature;
        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(CreatureParty playerParty, CreatureParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle)
        {
            // Wild Pokemon Battle
            playerUnit.Setup(playerParty.GetHealthyCreature());
            enemyUnit.Setup(wildCreature);

            dialogueBox.SetMoveNames(playerUnit.Creature.Moves);
            yield return dialogueBox.TypeDialogue($"A Wild {enemyUnit.Creature.Base.Name} appeared.");

        }
        else
        {
            // Trainer Battle

            // Show player and trainer sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);

            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogueBox.TypeDialogue($"{trainer.name} wants to battle");


            // Send out first pokemon of the trainer

            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyCreature = trainerParty.GetHealthyCreature();
            enemyUnit.Setup(enemyCreature);
            yield return dialogueBox.TypeDialogue($"{trainer.name} send out {enemyCreature.Base.Name}");


            // Send out first pokemon of the player

            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerCreature = playerParty.GetHealthyCreature();
            playerUnit.Setup(playerCreature);
            yield return dialogueBox.TypeDialogue($"Go {playerCreature.Base.Name}!");

            dialogueBox.SetMoveNames(playerUnit.Creature.Moves);
        }

        escapeAttempts = 0;
        partyScreen.Init();
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

    IEnumerator AboutToUse(Creature newCreature)
    {
        state = BattleState.Busy;
        yield return dialogueBox.TypeDialogue($"{trainer.Name} is about to use {newCreature.Base.Name}. Do you want to switch?");
        
        state = BattleState.AboutToUse;
        dialogueBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Creature creature, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogueBox.TypeDialogue($"Choose a move you want to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(creature.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
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
            if (playerAction == BattleAction.SwitchCreature)
            {
                var selectedCreature = playerParty.Creatures[currentMember];
                state = BattleState.Busy;
                yield return SwitchCreature(selectedCreature);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                dialogueBox.EnableActionSelector(false);
                yield return ThrowBall();
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
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
                yield return HandleCreatureFainted(targetUnit);
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
            yield return HandleCreatureFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
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

    IEnumerator HandleCreatureFainted(BattleUnit faintedUnit)
    {
        yield return dialogueBox.TypeDialogue($"{faintedUnit.Creature.Base.Name} Fainted");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            // Exp Gain
            int expYield = faintedUnit.Creature.Base.ExpYield;
            int enemyLevel = faintedUnit.Creature.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Creature.Exp += expGain;
            yield return dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} gained {expGain} exp");
            yield return playerUnit.Hud.SetExpSmooth();

            // Check Level Up
            while (playerUnit.Creature.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} grew to level {playerUnit.Creature.Level}");

                // Try to learn new move
                var newMove = playerUnit.Creature.GetLearnableMoveAtCurrentLevel();
                if (newMove != null)
                {
                    if (playerUnit.Creature.Moves.Count < CreatureBase.MaxNumOfMoves)
                    {
                        // Learn new move
                        playerUnit.Creature.LearnMove(newMove);
                        yield return dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} learned {newMove.Base.Name}");
                        dialogueBox.SetMoveNames( playerUnit.Creature.Moves );
                    }
                    else
                    {
                        // Forget an existing move
                        yield return dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} is trying to learn {newMove.Base.Name}");
                        yield return dialogueBox.TypeDialogue($"But it cannot learn more than {CreatureBase.MaxNumOfMoves} moves");
                        yield return ChooseMoveToForget(playerUnit.Creature, newMove.Base);
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(2);
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }

            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
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
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextCreature = trainerParty.GetHealthyCreature();
                if (nextCreature != null)
                {
                    // Send out next Creature
                    StartCoroutine(AboutToUse(nextCreature));
                }
                else
                    BattleOver(true);
            }
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
        else if(state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if(state == BattleState.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == CreatureBase.MaxNumOfMoves)
                {
                    // Don't learn new move
                    StartCoroutine(dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} did not learn {moveToLearn.Name}"));
                }
                else
                {
                    // Forget selected move and learn new move
                    var selectedMove = playerUnit.Creature.Moves[moveIndex].Base;
                    StartCoroutine(dialogueBox.TypeDialogue($"{playerUnit.Creature.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    playerUnit.Creature.Moves[moveIndex] = new Move(moveToLearn);
                }

                moveToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(onMoveSelected);
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
                StartCoroutine(RunTurns(BattleAction.UseItem));
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
                StartCoroutine(RunTurns(BattleAction.Run));
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
            if (playerUnit.Creature.HP <= 0)
            {
                partyScreen.SetMessageText("You must choose a Creature to continue the battle!");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.AboutToUse)
            {
                prevState = null;
                StartCoroutine(SendNextTrainerCreature());
            }
            else
                ActionSelection();
        }
    }

    void HandleAboutToUse()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice;

        dialogueBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogueBox.EnableChoiceBox(false);
            if (aboutToUseChoice)
            {
                // Yes, switch
                prevState = BattleState.AboutToUse;
                OpenPartyScreen();
            }
            else
            {
                // No, don't switch
                StartCoroutine(SendNextTrainerCreature());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableChoiceBox(false);
            // No, don't switch
            StartCoroutine(SendNextTrainerCreature());
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
        
        if (prevState == null)
        {
            state = BattleState.RunningTurn;
        }
        else if (prevState == BattleState.AboutToUse)
        {
            prevState = null;
            StartCoroutine(SendNextTrainerCreature());
        }
    }

    IEnumerator SendNextTrainerCreature()
    {
        state = BattleState.Busy;

        var nextCreature = trainerParty.GetHealthyCreature();
        enemyUnit.Setup(nextCreature);
        yield return dialogueBox.TypeDialogue($"{trainer.Name} sends out {nextCreature.Base.Name}");

        state = BattleState.RunningTurn;
    }

    IEnumerator ThrowBall()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue($"You can't steal the trainer's creature!");
            state = BattleState.RunningTurn;
            yield break;
        }

        yield return dialogueBox.TypeDialogue($"{player.Name} used Ball");

        var ballObj = Instantiate(ballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var ball = ballObj.GetComponent<SpriteRenderer>();

        // Animations
        yield return ball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return ball.transform.DOMoveY(enemyUnit.transform.position.y - 1, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchCreature(enemyUnit.Creature);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); i++)
        {
            yield return new WaitForSeconds(0.5f);
            yield return ball.transform.DOPunchRotation(new Vector3(0, 0, 10), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            // Creature is caught
            yield return dialogueBox.TypeDialogue($"{enemyUnit.Creature.Base.Name} was caught");
            yield return ball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddCreature(enemyUnit.Creature);
            
            yield return dialogueBox.TypeDialogue($"{enemyUnit.Creature.Base.Name} has been added to your party");

            Destroy(ball.gameObject);
            BattleOver(true);
        }
        else
        {
            // Creature broke out
            yield return new WaitForSeconds(1f);
            ball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakoutAnimation();

            if (shakeCount < 2)
                yield return dialogueBox.TypeDialogue($"{enemyUnit.Creature.Base.Name} broke free");
            else
                yield return dialogueBox.TypeDialogue($"Almost caught it");

            Destroy(ball.gameObject);
            state = BattleState.RunningTurn;
        }
    }

    int TryToCatchCreature(Creature creature)
    {
        float a = (3 * creature.MaxHp - 2 * creature.HP) * creature.Base.CatchRate * ConditionsDB.GetStatusBonus(creature.Status) / (3 * creature.MaxHp);

        if (a >= 255)
            return 4;

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            shakeCount++;
        }

        return shakeCount;
    }
    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue($"You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        escapeAttempts++;

        int playerSpeed = playerUnit.Creature.Speed;
        int enemySpeed = enemyUnit.Creature.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogueBox.TypeDialogue($"Ran away safely!");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogueBox.TypeDialogue($"Ran away safely!");
                BattleOver(true);
            }
            else
            {
                yield return dialogueBox.TypeDialogue($"Can't escape!");
                state = BattleState.RunningTurn;
            }
        }
    }

}