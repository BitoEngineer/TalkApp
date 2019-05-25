using UnityEngine;
using UnityEngine.SceneManagement;

public class TalkScript : MonoBehaviour
{
    public void TalkButonListener()
    {
        SceneManager.LoadScene("ChatScene");
    }
}
