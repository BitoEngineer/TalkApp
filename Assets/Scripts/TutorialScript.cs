using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    private bool? enableTutorial;

    public GameObject[] tutorialEnableObjects;
    public GameObject[] tutorialDisableObjects;

    void Start()
    {
        enableTutorial = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;
    }

    void Update()
    {
        if (enableTutorial.HasValue)
        {
            foreach (var obj in tutorialEnableObjects)
            {
                obj.SetActive(enableTutorial.Value);
            }
            foreach (var obj in tutorialDisableObjects)
            {
                obj.SetActive(!enableTutorial.Value);
            }

            PlayerPrefs.SetInt("TutorialEnabled", 0);
        }

        enableTutorial = null;
    }

    public void OnTurnTutorialOff()
    {
        enableTutorial = false;
    }
}
