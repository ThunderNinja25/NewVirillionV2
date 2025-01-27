using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

[CreateAssetMenu(fileName = "Creature", menuName = "Creature/Create new creature")]
public class CreatureBase : ScriptableObject
{
    [SerializeField] string _name;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;

    [SerializeField] CreatureType type1;
    [SerializeField] CreatureType type2;

    //Base Stats
    [SerializeField] int maxHp;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int spAttack;
    [SerializeField] int spDefense;
    [SerializeField] int speed;

    [SerializeField] List<LearnableMove> learnableMoves;

    public string Name { get { return _name; } }
    public string Description { get { return description; } }
    public Sprite FrontSprite { get { return frontSprite; } }
    public Sprite BackSprite { get { return backSprite; } }
    public CreatureType Type1 { get {  return type1; } }
    public CreatureType Type2 { get {  return type2; } }
    public int MaxHp { get { return maxHp; } }
    public int Attack { get { return attack; } }
    public int Defense { get {  return defense; } }
    public int SpAttack { get {  return spAttack; } }
    public int SpDefense { get { return spDefense; } }
    public int Speed { get { return speed; } }
    public List<LearnableMove> LearnableMoves { get { return learnableMoves; } }
}

[System.Serializable]
public class LearnableMove
{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base { get { return moveBase;} }
    public int Level { get { return level; } }
}

public enum CreatureType
{
    None,
    Normal,
    Fire,
    Water,
    Electric,
    Grass,
    Ice,
    Fighting,
    Poison,
    Ground,
    Flying,
    Psychic,
    Bug,
    Rock,
    Ghost,
    Dragon
}

public enum Stat
{
    Attack,
    Defense,
    SpAttack,
    SpDefense,
    Speed,

    //These 2 are not actual stats, they're used to boost moveAccuracy
    Accuracy,
    Evasion
}

public class TypeChart
{
    static float[][] chart =
    {
        //                   NOR FIR  WAT ELE GRA ICE FIG POI GRO FLY PSY BUG ROC GHO DRA
        /*NOR*/ new float[] { 1f,  1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,0.5f, 0f, 1f },
        /*FIR*/ new float[] { 1f,0.5f,0.5f,1f, 2f, 2f, 1f, 1f, 1f, 1f, 1f, 2f,0.5f,1f, 0.5f },
        /*WAT*/ new float[] { 1f, 2f,0.5f, 1f,0.5f,1f, 1f, 1f, 2f, 1f, 1f, 1f, 2f, 1f, 0.5f },
        /*ELE*/ new float[] { 1f, 1f, 2f,0.5f,0.5f,1f, 1f, 1f, 0f, 2f, 1f, 1f, 1f, 1f, 0.5f },
        /*GRA*/ new float[] { 1f,0.5f,2f, 1f, 0.5f,1f, 1f,0.5f,2f,0.5f,1f,0.5f,2f, 1f, 0.5f },
        /*ICE*/ new float[] { 1f,0.5f,0.5f,1f, 2f,0.5f,1f, 1f, 2f, 2f, 1f, 1f, 1f, 1f, 2f },
        /*FIG*/ new float[] { 2f, 1f, 1f, 1f, 1f, 2f,  1f,0.5f,1f,0.5f,0.5f,0.5f,2f, 0f, 1f },
        /*POI*/ new float[] { 1f, 1f, 1f, 1f, 2f, 1f, 1f, 0.5f,0.5f,1f,1f, 1f,0.5f,0.5f, 1f },
        /*GRO*/ new float[] { 1f, 2f, 1f, 2f,0.5f, 1f, 1f, 2f, 1f, 0f, 1f,0.5f, 2f, 1f, 1f },
        /*FLY*/ new float[] { 1f, 1f, 1f,0.5f, 2f, 1f, 2f, 1f, 1f, 1f, 1f, 2f,0.5f, 1f, 1f },
        /*PSY*/ new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 2f, 1f, 0.5f, 1f, 1f, 1f, 1f },
        /*BUG*/ new float[] { 1f,0.5f, 1f, 1f, 2f,1f,0.5f,0.5f,1f,0.5f,2f, 1f, 1f, 0.5f, 1f },
        /*ROC*/ new float[] { 1f, 2f, 1f, 1f, 1f, 2f,0.5f, 1f,0.5f,2f, 1f, 2f, 1f, 1f, 1f },
        /*GHO*/ new float[] { 0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 2f, 1f },
        /*DRA*/ new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f }
    };

    public static float GetEffectiveness(CreatureType attackType, CreatureType defenseType)
    {
        if (attackType == CreatureType.None || defenseType == CreatureType.None)
            return 1;

        int row = (int)attackType - 1;
        int col = (int)defenseType - 1;

        return chart[row][col]; 
    }
}
