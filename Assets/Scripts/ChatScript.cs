using Assets.Core;
using Assets.Core.Models;
using Assets.Core.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatScript : MonoBehaviour
{

    public GameObject MeMessage;
    public GameObject YouMessage;
    public GameObject MessageContainer;
    public GameObject InputField;

    private TalkClient _client;

    void Start()
    {
        _client = FindObjectOfType<TalkScript>().Client;
        _client.AddCallback(ContentType.SendMessage, onMessageReceived);
    }

    public void OnTextEnded(string txt)
    {
        _client.Send(ContentType.SendMessage, new Message() { Text = txt }, (p) =>
        {
            if (p.ContentResult == ContentResult.OK)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    var go = Instantiate(MeMessage, MessageContainer.transform);
                    go.GetComponentInChildren<Text>().text = txt;

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

    public void Back()
    {
        _client.Send(ContentType.EndTalk, null, (p) =>
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
}
