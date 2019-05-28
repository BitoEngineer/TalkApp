using System;
using Assets.Core;
using Assets.Core.Extensions;
using Assets.Core.Models;
using Assets.Core.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TalkScript : MonoBehaviour
{

    public TalkClient Client { get; set; } = null;
    public enum StateEnum { Connecting, Loading, ReadyToRun, Running, ConnectionLost, UnexpectedReply }
    public StateEnum State { get; protected set; } = StateEnum.Connecting;

    private static string SERVER_IP = "40.85.119.162";//"192.168.1.73";//"localhost";// "runsnailrun.servebeer.com";
    private static int SERVER_PORT = 2222;

    void Start()
    {
        Client = new TalkClient();
        StartConnectionToServer();
        Client.Login();
    }

    public void TalkButonListener()
    {
        Client.StartTalk((p) =>
        {
            try
            {

                Debug.Log("StartTalk Response 1");
                if (p.ContentResult == ContentResult.OK)
                {
                    Debug.Log("StartTalk Response 2");
                    var foundTalker = p.DeserializeContent<TalkRequest>().Accepted;

                    Debug.Log("StartTalk Response 3");
                    if (foundTalker)
                    {
                        Debug.Log("StartTalk Response 4");
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Debug.Log("StartTalk Response 6");
                            SceneManager.LoadScene("ChatScene");
                            Debug.Log("StartTalk Response 6.1");
                        });
                    }
                    else
                    {
                        Debug.Log("StartTalk Response 5");
                        Client.AddCallback(ContentType.StartTalk, (packet) =>
                        {
                            Debug.Log("StartTalk Response 7");
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                Debug.Log("StartTalk Response 8");
                                SceneManager.LoadScene("ChatScene");
                                Debug.Log("StartTalk Response 8.1");
                            });
                        });
                    }

                    Debug.Log("StartTalk Response 9");
                }
            }
            catch (Exception e)
            {
                Debug.Log("StartTalk Error");
                Debug.Log($"StartTalk Error - {e.ToString()}");
            }
        });

        Debug.Log("StartTalk Response 10");
    }

    void OnApplicationQuit()
    {
        Client.Send(ContentType.Logout, string.Empty);
    }

    private void StartConnectionToServer()
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

    private void OnConectivityChanged(bool isConnected)
    {
        if (!isConnected)
        {
            State = StateEnum.ConnectionLost;
        }
        else if (State == StateEnum.Connecting)
        {
            //TODO
        }
    }
}
