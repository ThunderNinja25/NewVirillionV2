using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;

    PartyMemberUI[] memberSlots;
    List<Creature> creatures;

    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>();
    }

    public void SetPartyData(List<Creature> creatures)
    {
        this.creatures = creatures;

        for ( int i =0; i < memberSlots.Length; i++ )
        {
            if(i < creatures.Count)
            {
                memberSlots[i].SetData(creatures[i]);
            }
            else
            {
                memberSlots[i].gameObject.SetActive(false);
            }
        }
        messageText.text = "Choose a Creature";
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        for(int i = 0; i < creatures.Count; i++)
        {
            if(i ==  selectedMember)
            {
                memberSlots[i].SetSelected(true);
            }
            else
            {
                memberSlots[i].SetSelected(false);
            }
        }
    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
