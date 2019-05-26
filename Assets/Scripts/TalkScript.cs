using System;
using Assets.Core;
using Assets.Core.Extensions;
using Assets.Core.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TalkScript : MonoBehaviour
{

    public TalkClient Client { get; set; } = null;
    public enum StateEnum { Connecting, Loading, ReadyToRun, Running, ConnectionLost, UnexpectedReply }
    public StateEnum State { get; protected set; } = StateEnum.Connecting;

    private static string SERVER_IP = "192.168.1.12";//"localhost";// "40.85.119.162";//"runsnailrun.servebeer.com";
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
            if (p.ContentResult == ContentResult.OK)
            {
                var foundTalker = p.DeserializeContent<bool>();

                if (foundTalker)
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => SceneManager.LoadScene("ChatScene"));
                }
                else
                {
                    Client.AddCallback(ContentType.StartTalk, (packet) =>
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => SceneManager.LoadScene("ChatScene"));
                    });
                }
            }
        });
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
