using Assets.Core.Extensions;
using Assets.Core.Models;
using Assets.Core.Server;
using System;
using TMPro;
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

    private bool _conversationEnded = false;

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
        else if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            SendText();
        }

        if (_conversationEnded && tmp_inputField.IsInteractable())
        {
            tmp_inputField.interactable = false;
        }
        else if(!_conversationEnded && !tmp_inputField.IsInteractable())
        {
            tmp_inputField.interactable = true;
        }

        InputField.GetComponent<LayoutElement>().preferredHeight =
            TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : -1;
    }

    public void SendText()
    {
        var text = tmp_inputField.text;

        if (_conversationEnded || string.IsNullOrWhiteSpace(text))
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
        if (_conversationEnded)
        {
            return;
        }

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
            _conversationEnded = true;
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

    public void OnNextTalker()
    {
        _conversationEnded = true;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            var go = Instantiate(InfoMessage, MessageContainer.transform);
            go.GetComponentInChildren<TMP_Text>().text = "Searching for a new talker...";
        });

        TalkScript.Client.Send(ContentType.EndTalk, null, _ =>
        {
            SendStartTalk();
        });
    }

    private void SendStartTalk()
    {
        TalkScript.Client.StartTalk((p) =>
        {
            try
            {
                if (p.ContentResult != ContentResult.OK)
                {
                    Debug.Log($"StartTalk Unexpected ContentResult - {p.ContentResult}");
                    OnError();
                }
                else
                {
                    TalkRequest requestReply;

                    p.TryDeserializeContent<TalkRequest>(out requestReply);

                    if (requestReply is object && requestReply.Accepted)
                    {
                        OnTalkerFound();
                    }
                    else
                    {
                        OnTalkerNotFound();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"StartTalk Error - {e.ToString()}");
                OnError();
            }
        });
    }

    private void OnTalkerNotFound()
    {
        TalkScript.Client.AddCallback(ContentType.StartTalk, (packet) =>
        {
            TalkScript.Client.RemoveCallback(ContentType.StartTalk);
            OnTalkerFound();
        });
    }

    private void OnTalkerFound()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            ClearMessages();
            var go = Instantiate(InfoMessage, MessageContainer.transform);
            go.GetComponentInChildren<TMP_Text>().text = "New talker found!";
            _conversationEnded = false;
        });
    }

    private void ClearMessages()
    {
        foreach(Transform transform in MessageContainer.transform)
        {
            Destroy(transform.gameObject);
        }
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

    private void OnError()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            SceneManager.LoadScene("TalkScene");
        });
    }
}
