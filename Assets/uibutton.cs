using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButton : MonoBehaviour
{
    public void LoadInformationScene()
    {
        SceneManager.LoadScene("Information");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Game");
    }
}