using UnityEngine;

public class TrainerFOV : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player)
    {
        GameManager.Instance.OnEnterTrainersView(GetComponentInParent<TrainerController>());
    }
}
