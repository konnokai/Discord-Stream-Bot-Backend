using System;

namespace DiscordStreamBotBackend.Model
{
    public class BililiveRecorderWebHookJson
    {
        public string EventType { get; set; }
        public DateTime EventTimestamp { get; set; }
        public string EventId { get; set; }
        public BililiveRecorderWebHookEventData EventData { get; set; }
    }

    public class BililiveRecorderWebHookEventData
    {
        public int RoomId { get; set; }
        public int ShortId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string AreaNameParent { get; set; }
        public string AreaNameChild { get; set; }
        public bool Recording { get; set; }
        public bool Streaming { get; set; }
        public bool DanmakuConnected { get; set; }
        public string SessionId { get; set; }
        public string RelativePath { get; set; }
        public DateTime FileOpenTime { get; set; }
        public int FileSize { get; set; }
        public double Duration { get; set; }
        public DateTime FileCloseTime { get; set; }
    }
}
