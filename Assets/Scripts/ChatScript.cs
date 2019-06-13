using Assets.Core.Models;
using Assets.Core.Server;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatScript : MonoBehaviour
{

    public GameObject MeMessage;
    public GameObject YouMessage;
    public GameObject InfoMessage;
    public GameObject MessageContainer;
    public GameObject InputField;
    public GameObject ShowMessagesButton;
    public GameObject HideMessagesButton;

    private bool _talkerLeft = false;

    private TMP_InputField tmp_inputField;

    void Start()
    {
        TalkScript.Client.AddCallback(ContentType.SendMessage, onMessageReceived);
        TalkScript.Client.AddCallback(ContentType.EndTalk, onTalkerLeft);

        tmp_inputField = InputField.GetComponentInChildren<TMP_InputField>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Back();
        }
        else if (_talkerLeft && tmp_inputField.IsInteractable())
        {
            tmp_inputField.interactable = false;
        }
    }

    public void SendText()
    {
        var text = tmp_inputField.text;

        if (_talkerLeft || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        TalkScript.Client.Send(ContentType.SendMessage, new Message() { Text = text }, (p) =>
        {
            if (p.ContentResult == ContentResult.OK)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    var go = Instantiate(MeMessage, MessageContainer.transform);
                    go.GetComponentInChildren<TMP_Text>().text = text;

                    tmp_inputField.text = string.Empty;
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
                go.GetComponentInChildren<TMP_Text>().text = message.Text;
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
                go.GetComponentInChildren<TMP_Text>().text = "Talker has left";
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
