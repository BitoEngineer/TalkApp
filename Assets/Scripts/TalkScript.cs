using System;
using Assets.Core;
using Assets.Core.Extensions;
using Assets.Core.Models;
using Assets.Core.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TalkScript : MonoBehaviour
{
    public GameObject LoadingObject;
    public GameObject ConnectedTalker;

    private static bool isToUpdate = false;
    private static int numTalkers = 0;

    public static TalkClient Client { get; set; } = null;

    private static string SERVER_IP = "40.85.119.162";//"192.168.1.73";//"localhost";// "runsnailrun.servebeer.com";
    private static int SERVER_PORT = 2222;

    static TalkScript()
    {
        Client = new TalkClient();
        StartConnectionToServer();
        Client.AddCallback(ContentType.ConnectedTalkers, onConnectedTalker);
        Client.Login();
    }

    void Start()
    {
        LoadingObject?.SetActive(false);
        updateNrTalkers();
    }

    void Update()
    {
        if (isToUpdate)
        {
            isToUpdate = false;
            updateNrTalkers();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void updateNrTalkers()
    {
        ConnectedTalker.GetComponent<Text>().text = $"{numTalkers} talkers online";
    }

    public void TalkButonListener()
    {
        LoadingObject?.SetActive(true);

        Client.StartTalk((p) =>
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
                    var foundTalker = p.DeserializeContent<TalkRequest>().Accepted;

                    if (foundTalker)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            SceneManager.LoadScene("ChatScene");
                        });
                    }
                    else
                    {
                        Client.AddCallback(ContentType.StartTalk, (packet) =>
                        {
                            Client.RemoveCallback(ContentType.StartTalk);
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                SceneManager.LoadScene("ChatScene");
                            });
                        });
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

    void OnApplicationQuit()
    {
        Client.Send(ContentType.Logout, string.Empty);
    }

    private static void onConnectedTalker(JsonPacket p)
    {
        if (p.ContentResult == ContentResult.OK)
        {
            var talkers = p.DeserializeContent<ConnectedTalkers>();
            numTalkers = talkers.Connected - 1;
            isToUpdate = true;
        }
    }

    private static void StartConnectionToServer()
    {
        Client.Stop();
#if DEBUG
        Client.RequestTimeout = 100000;
#endif
        Client.LogDebugEvent -= LogDebug;
        Client.LogErrorEvent -= LogError;

        Client.LogDebugEvent += LogDebug;
        Client.LogErrorEvent += LogError;

        Client.OnConnectivityChange += OnConectivityChanged;

        Client.Start(SERVER_IP, SERVER_PORT, Guid.NewGuid().ToString("N"));
    }

    private static void LogDebug(string message, params object[] args)
    {
        Debug.Log("SERVER: " + string.Format(message, args));
    }

    private static void LogError(Exception e, string message, params object[] args)
    {
        Debug.LogError("SERVER ERROR: " + string.Format(message, args) + e?.ToString());
    }

    private static void OnConectivityChanged(bool isConnected)
    {
        // TODO
    }

    private static void OnError()
    {
        // TODO
    }
}
