using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] List<Creature> wildCreatures;

    public Creature GetRandomWildCreature()
    {
        var wildCreature = wildCreatures[Random.Range(0, wildCreatures.Count)];
        wildCreature.Init();
        return wildCreature;
    }
}
