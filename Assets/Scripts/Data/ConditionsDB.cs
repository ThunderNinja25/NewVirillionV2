using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB 
{
    public static void Init()
    {
        foreach(var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {ConditionID.psn, new Condition()
        {
            Name = "Poison",
            StartMessage = "has been poisoned",
            OnAfterTurn = (Creature creature) =>
            {
                creature.UpdateHP(creature.MaxHp / 8);
                creature.StatusChanges.Enqueue($"{creature.Base.Name} hurt itself due to poison");
            }
        }
        },
        {ConditionID.brn, new Condition()
        {
            Name = "Burn",
            StartMessage = "has been burned",
            OnAfterTurn = (Creature creature) =>
            {
                creature.UpdateHP(creature.MaxHp / 16);
                creature.StatusChanges.Enqueue($"{creature.Base.Name} was burned");
            }
        }
        },
        {ConditionID.par, new Condition()
        {
            Name = "Paralyzed",
            StartMessage = "has been paralyzed",
            OnBeforeMove = (Creature creature) =>
            {
                if( Random.Range(1, 5) == 1)
                {
                    creature.StatusChanges.Enqueue($"{creature.Base.Name} is paralyzed and can't move");
                    return false;
                }

                return true;
            }
        }
        },
        {ConditionID.frz, new Condition()
        {
            Name = "Freeze",
            StartMessage = "has been frozen",
            OnBeforeMove = (Creature creature) =>
            {
                if( Random.Range(1, 5) == 1)
                {
                    creature.CureStatus();
                    creature.StatusChanges.Enqueue($"{creature.Base.Name} is not frozen anymore");
                    return true;
                }

                return false;
            }
        }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep",
                OnStart = (Creature creature) =>
                {
                    creature.StatusTime = Random.Range(1, 4);
                    Debug.Log($"Will be asleep for {creature.StatusTime} moves");
                },
                OnBeforeMove = ( Creature creature) =>
                {
                    if(creature.StatusTime <= 0)
                    {
                        creature.CureStatus();
                        creature.StatusChanges.Enqueue($"{creature.Base.Name} woke up!");
                        return true;
                    }

                    creature.StatusTime--;
                    creature.StatusChanges.Enqueue($"{creature.Base.Name} is sleeping");
                    return false;
                }
            }
        },

        { ConditionID.confusion,
        new Condition()
        {
            Name = "Confusion",
            StartMessage = "has been confused",
            OnStart = (Creature creature) =>
            {
                creature.VolatileStatusTime = Random.Range(1, 5);
                Debug.Log($"Will be confused for {creature.VolatileStatusTime} moves");
            },
            OnBeforeMove = (Creature creature) =>
            {
                if(creature.VolatileStatusTime <= 0)
                {
                    creature.CureVolatileStatus();
                    creature.StatusChanges.Enqueue($"{creature.Base.Name} is no longer confused!");
                    return true;
                }
                creature.VolatileStatusTime--;

                if(Random.Range(1, 3) == 1)
                    return true;

                creature.StatusChanges.Enqueue($"{creature.Base.Name} is confused");
                creature.UpdateHP(creature.MaxHp / 8);
                creature.StatusChanges.Enqueue($"{creature.Base.Name} hurt itself in it's confusion");
                return false;
            }
        }
        }
    };

}

public enum ConditionID
{
    none, psn, brn, slp, par, frz,
    confusion
}
