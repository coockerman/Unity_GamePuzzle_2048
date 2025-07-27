using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadSceneGameplay(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
