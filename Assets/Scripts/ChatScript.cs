using Assets.Core.Models;
using Assets.Core.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatScript : MonoBehaviour
{

    public GameObject MeMessage;
    public GameObject YouMessage;
    public GameObject InfoMessage;
    public GameObject MessageContainer;
    public GameObject InputField;
    public GameObject ShowMessagesButton;
    public GameObject HideMessagesButton;

    private string _text;
    private bool _talkerLeft = false;

    void Start()
    {
        TalkScript.Client.AddCallback(ContentType.SendMessage, onMessageReceived);
        TalkScript.Client.AddCallback(ContentType.EndTalk, onTalkerLeft);
    }

    public void OnTextUpdated(string txt)
    {
        _text = txt;
    }

    public void OnTextEnded(string txt)
    {
        _text = txt;
        SendText();
    }

    public void SendText()
    {
        if (_talkerLeft || string.IsNullOrWhiteSpace(_text))
        {
            return;
        }

        TalkScript.Client.Send(ContentType.SendMessage, new Message() { Text = _text }, (p) =>
        {
            if (p.ContentResult == ContentResult.OK)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    var go = Instantiate(MeMessage, MessageContainer.transform);
                    go.GetComponentInChildren<Text>().text = _text;

                    var inputfield = InputField.GetComponentInChildren<InputField>();
                    inputfield.Select();
                    inputfield.text = null;
                    //InputField.GetComponentInChildren<InputField>().ActivateInputField();
                });
            }
        });
    }

    public void onMessageReceived(JsonPacket p)
    {
        if (p.ContentResult == ContentResult.OK)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var message = p.DeserializeContent<Message>();
                var go = Instantiate(YouMessage, MessageContainer.transform);
                go.GetComponentInChildren<Text>().text = message.Text;
            });
        }
    }

    public void onTalkerLeft(JsonPacket p)
    {
        if (p.ContentResult == ContentResult.OK)
        {
            _talkerLeft = true;
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var message = p.DeserializeContent<Message>();
                var go = Instantiate(InfoMessage, MessageContainer.transform);
                go.GetComponentInChildren<Text>().text = "Talker has left";
            });
        }
    }

    public void Back()
    {
        TalkScript.Client.Send(ContentType.EndTalk, null, (p) =>
        {
            if (p.ContentResult == ContentResult.OK)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    SceneManager.LoadScene("TalkScene");
                });
            }
        });
    }

    public void ShowTextListener()
    {
        MessageContainer.SetActive(true);
        ShowMessagesButton.SetActive(false);
        HideMessagesButton.SetActive(true);
    }

    public void HideTextListener()
    {
        MessageContainer.SetActive(false);
        HideMessagesButton.SetActive(false);
        ShowMessagesButton.SetActive(true);
    }
}
