using Assets.Core.Models;
using Assets.Core.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Core
{
    public class TalkClient
    {
        #region Log

        public delegate void LogDebug(string message, params object[] args);
        public delegate void LogError(Exception e, string message, params object[] args);

        public LogDebug LogDebugEvent { get; set; }
        public LogError LogErrorEvent { get; set; }

        #endregion

        public string ServerName { get; private set; }
        public int ServerPort { get; private set; }
        public string ClientId { get; set; }
        public int RequestTimeout { get; set; }

        public enum CallResult { Timedout, ServerUnavailable, Success }
        public delegate void ReplyReceivedCallback(JsonPacket p);
        public delegate void FailureCallback(JsonPacket p, CallResult result);
        public delegate void PacketCallback(JsonPacket p);

        public bool IsConnected { get { return (socket != null) && socket.Connected; } }

        private bool? lastConnectivityState = null;
        public Action<bool> OnConnectivityChange;


        private TcpClient socket;
        private const int MAX_BYTES = 1024 * 5;
        private byte[] bufferCallback = new byte[MAX_BYTES];
        private List<byte> buffer = new List<byte>();

        private ConcurrentDictionary<int, ReplyHandler> waitingReply = new ConcurrentDictionary<int, ReplyHandler>();
        private ConcurrentDictionary<int, Timer> waitingTimers = new ConcurrentDictionary<int, Timer>();
        private ConcurrentDictionary<ContentType, PacketCallback> callbacks = new ConcurrentDictionary<ContentType, PacketCallback>();

        public TalkClient()
        {
            RequestTimeout = 5000;
        }

        public virtual void Start(string ip, int port, string clientId)
        {
            ServerName = ip;
            ServerPort = port;
            ClientId = clientId;

            startConnect();
            if (LogDebugEvent != null) LogDebugEvent.Invoke("Starting Client \"{0}\" on {1}:{2}", clientId, ip, port);
        }

        public virtual void Stop()
        {
            try
            {
                socket.Close();
                socket = null;
                if (LogDebugEvent != null) LogDebugEvent.Invoke("Client closed");
            }
            catch (Exception e)
            {
                if (LogDebugEvent != null) LogErrorEvent.Invoke(e, "Error stopping Client");
            }
        }

        /// <summary>
        /// Send data to server.
        /// </summary>
        /// <param name="contentType">Server endpoint URI</param>
        /// <param name="o">Request content object</param>
        /// <param name="okCallback">Reply callback when result code is Ok</param>
        /// <param name="okResults">Collection of result codes to be considered for <paramref name="okCallback"/>, only <see cref="ContentResult.Ok"/> will be considered in case of null</param>
        /// <param name="nokCallback">Reply callback when result code is not Ok, or not present in the <paramref name="okResults"/></param>
        /// <param name="fail">Fail callback, when the request is not even reaching the server</param>
        /// <param name="timeoutMs">Request timeout in milliseconds. If negative, the <see cref="MyServClient.RequestTimeout"/> will be used instead. If 0, it will never call <paramref name="fail"/> by timeout.</param>
        public void Send(ContentType contentType, object o, ReplyReceivedCallback okCallback = null,
            ICollection<ContentResult> okResults = null, ReplyReceivedCallback nokCallback = null,
            FailureCallback fail = null, int timeoutMs = -1)
        {
            JsonPacket p = new JsonPacket(ClientId, contentType.ToString(), JsonConvert.SerializeObject(o));

            if (okCallback != null || fail != null)
            {
                var handler = new ReplyHandler(okCallback, nokCallback, okResults, fail);
                waitingReply.AddOrUpdate(p.PacketID, handler, (x, y) => handler);

                if (timeoutMs != 0)
                {
                    if (timeoutMs < 0)
                    {
                        timeoutMs = RequestTimeout;
                    }

                    Timer t = new Timer(timeElapsed, p, timeoutMs, timeoutMs);
                    waitingTimers.AddOrUpdate(p.PacketID, t, (x, y) => t);
                }
            }

            startSending(p);
        }

        public void AddCallback(ContentType contentType, PacketCallback callback)
        {
            callbacks.AddOrUpdate(contentType, callback, (ct, c) => callback);
        }

        public void RemoveCallback(ContentType contentType)
        {
            PacketCallback callback;
            callbacks.TryRemove(contentType, out callback);
        }

        #region Private

        private class ReplyHandler
        {
            private ICollection<ContentResult> OkResults;
            private ReplyReceivedCallback OkCallback;
            private ReplyReceivedCallback NokCallback;
            private FailureCallback FailureCallback;
            public ReplyHandler(ReplyReceivedCallback okCallback, ReplyReceivedCallback nokCallback,
                ICollection<ContentResult> okResults, FailureCallback failureCallback)
            {
                OkResults = okResults;
                OkCallback = okCallback;
                NokCallback = nokCallback;
                FailureCallback = failureCallback;
            }

            internal void OnFail(JsonPacket p, CallResult reason)
            {
                if (FailureCallback != null)
                {
                    FailureCallback.Invoke(p, reason);
                }
            }

            internal void OnCallback(JsonPacket p)
            {
                bool isOk = (OkResults == null && p.ContentResult == ContentResult.OK);
                isOk = isOk || OkResults.Contains(p.ContentResult);

                if (isOk)
                {
                    if (OkCallback != null)
                    {
                        OkCallback.Invoke(p);
                    }
                }
                else
                {
                    if (NokCallback != null)
                    {
                        NokCallback.Invoke(p);
                    }
                }
            }
        }

        private void onEndSending(object op, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                process((JsonPacket)args.UserToken, CallResult.ServerUnavailable);
            }
        }

        protected virtual void startSending(JsonPacket p)
        {
            var data = p.GetPacket();
            var args = new SocketAsyncEventArgs();

            args.SetBuffer(data, 0, data.Length);
            args.Completed += new EventHandler<SocketAsyncEventArgs>(onEndSending);
            args.UserToken = p;

            if (!socket.Client.SendAsync(args))
            {
                onEndSending(this, args);
            }
        }

        private void timeElapsed(object op)
        {
            JsonPacket p = (JsonPacket)op;

            ReplyHandler r;

            if (waitingReply.TryRemove(p.PacketID, out r))
            {
                r.OnFail(p, CallResult.Timedout);
            }

            Timer t;

            if (waitingTimers.TryRemove(p.PacketID, out t))
            {
                t.Dispose();
            }
        }

        protected void notifyConnectivityState(bool newState)
        {
            if (OnConnectivityChange != null && lastConnectivityState != newState)
            {
                OnConnectivityChange(newState);
                lastConnectivityState = newState;
            }
        }

        private void startConnect()
        {
            if (LogDebugEvent != null) LogDebugEvent.Invoke("Start connection to {0}:{1}...", ServerName, ServerPort);

            socket = new TcpClient();
            socket.ReceiveTimeout = RequestTimeout;
            socket.SendTimeout = RequestTimeout;

            Task.Factory.StartNew(connectAction)
                .ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                        if ((socket != null) && !IsConnected) { startConnect(); }
                    }
                });
        }

        private bool connectAction()
        {
            if (!Monitor.TryEnter(this))
            {
                return false;
            }

            try
            {
                if (LogDebugEvent != null) LogDebugEvent.Invoke("Connecting to {0}:{1}...", ServerName, ServerPort);

                socket.Connect(ServerName, ServerPort);

                if (socket.Connected)
                {
                    if (LogDebugEvent != null) LogDebugEvent.Invoke("Connected to {0}:{1}", ServerName, ServerPort);

                    notifyConnectivityState(true);
                    startReceiving();
                }
                else
                {
                    if (LogDebugEvent != null) LogDebugEvent.Invoke("Can't connect to {0}:{1}", ServerName, ServerPort);
                    notifyConnectivityState(false);
                }

                return true;
            }
            catch (Exception e)
            {
                if (LogDebugEvent != null) LogDebugEvent.Invoke("Can't connect to {0}:{1} - {2}", ServerName, ServerPort, e.Message);

                notifyConnectivityState(false);

                return true;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        private void startReceiving()
        {
            try
            {
                socket.Client.BeginReceive(bufferCallback, 0, bufferCallback.Length, SocketFlags.None, new AsyncCallback(receiveCallback), this);
            }
            catch (Exception e)
            {
                if (LogDebugEvent != null) LogErrorEvent.Invoke(e, "Error start receiving data - reconnecting");
                startConnect();
            }
        }

        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = socket.Client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //LogDebugEvent?.Invoke("{0} bytes received", bytesRead);

                    buffer.AddRange(bufferCallback.Take(bytesRead));

                    while (processReceivedData()) ;
                }

                startReceiving();
            }
            catch (SocketException e)
            {
                if (LogDebugEvent != null) LogErrorEvent.Invoke(e, "Error on socket - reconnecting");
                startConnect();
            }
            catch (ObjectDisposedException e)
            {
                if (LogDebugEvent != null) LogErrorEvent.Invoke(e, "Error socket closed - reconnecting");
                startConnect();
            }
            catch (Exception e)
            {
                if (LogDebugEvent != null) LogErrorEvent.Invoke(e, "Error receiving data");
                startReceiving();
            }
        }

        private bool processReceivedData()
        {
            JsonPacket p;

            if (JsonPacket.DeserializePacket(buffer, out p))
            {
                if (!string.IsNullOrEmpty(p.ContentJson))
                {
                    Task.Run(() => process(p));
                }

                return true;
            }

            return false;
        }

        protected void process(JsonPacket p, CallResult result = CallResult.Success)
        {
            if (LogDebugEvent != null) LogDebugEvent.Invoke("Packet received: {0}", p.ToString());

            Timer t;
            if (waitingTimers.TryRemove(p.PacketID, out t))
            {
                t.Dispose();
            }

            ReplyHandler r;

            if (waitingReply.TryRemove(p.PacketID, out r))
            {
                r.OnCallback(p);
                return;
            }

            PacketCallback c;

            if (callbacks.TryGetValue((ContentType)Enum.Parse(typeof(ContentType), p.ContentType), out c))
            {
                c(p);
            }
        }

        #endregion
    }
}
