namespace Assets.Core.Extensions
{
    public static class TalkClientExtenions
    {
        public static void Login(this TalkClient @this)
        {
            @this.Send(ContentType.Login, string.Empty);
        }

        public static void StartTalk(this TalkClient @this, TalkClient.ReplyReceivedCallback cb)
        {
            @this.Send(ContentType.StartTalk, string.Empty, cb);
        }
    }
}
