using UnityEngine;
using UnityEngine.UI;

public class ChatScript : MonoBehaviour
{
    public GameObject MeMessage;
    public GameObject YouMessage;
    public GameObject MessageContainer;
    public GameObject InputField;

    private int i = 0;
    public void OnTextEnded(string txt)
    {
        var go = Instantiate(i++ % 2 == 0 ? MeMessage : YouMessage, MessageContainer.transform);
        go.GetComponentInChildren<Text>().text = txt;

        //InputField.GetComponentInChildren<InputField>().text = "";
        //InputField.GetComponentInChildren<Text>().text = "";
        InputField.GetComponentInChildren<InputField>().ActivateInputField();
    }
}
