using UnityEditor.Build.Content;
using UnityEngine;

public class EssentialObjectsSpawner : MonoBehaviour
{
    [SerializeField] GameObject essentialObjectPrefab;

    private void Awake()
    {
        var existingObjects = FindObjectsOfType<EssentialObjects>();

        if(existingObjects.Length == 0)
        {
            Instantiate(essentialObjectPrefab, new Vector3(0,0,0), Quaternion.identity);
        }
    }
}
