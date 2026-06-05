using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour 
{
    [SerializeField] List<SceneDetails> connectedScenes;

    public bool isLoaded {  get; private set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Debug.Log($"Entered {gameObject.name}");

            LoadScene();
            GameManager.Instance.SetCurrentScene(this);

            // Load all connected scenes
            foreach (var scene in connectedScenes)
            {
                scene.LoadScene();
            }

            // Unload the scenes that are no longer connected
            if (GameManager.Instance.previousScene != null)
            {
                var previouslyLoadedScenes = GameManager.Instance.previousScene.connectedScenes;
                foreach (var scene in previouslyLoadedScenes)
                {
                    if (!connectedScenes.Contains(scene) && scene != this)
                    {
                        scene.UnloadScene();
                    }
                }
            }
        }
    }

    public void LoadScene()
    {
        if (!isLoaded)
        {
            SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);

            isLoaded = true;
        }
    }

    public void UnloadScene()
    {
        if (isLoaded)
        {
            SceneManager.UnloadSceneAsync(gameObject.name);
            isLoaded = false;
        }
    }
}
