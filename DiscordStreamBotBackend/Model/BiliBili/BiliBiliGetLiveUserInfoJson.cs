using Newtonsoft.Json;
using System.Collections.Generic;

namespace DiscordStreamBotBackend.Model.BiliBili
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class GetLiveUserInfo
    {
        [JsonProperty("info")]
        public Info Info { get; set; }

        [JsonProperty("exp")]
        public Exp Exp { get; set; }

        [JsonProperty("follower_num")]
        public int FollowerNum { get; set; }

        [JsonProperty("room_id")]
        public int RoomId { get; set; }

        [JsonProperty("medal_name")]
        public string MedalName { get; set; }

        [JsonProperty("glory_count")]
        public int GloryCount { get; set; }

        [JsonProperty("pendant")]
        public string Pendant { get; set; }

        [JsonProperty("link_group_num")]
        public int LinkGroupNum { get; set; }

        [JsonProperty("room_news")]
        public RoomNews RoomNews { get; set; }
    }

    public class Exp
    {
        [JsonProperty("master_level")]
        public MasterLevel MasterLevel { get; set; }
    }

    public class Info
    {
        [JsonProperty("uid")]
        public int Uid { get; set; }

        [JsonProperty("uname")]
        public string Uname { get; set; }

        [JsonProperty("face")]
        public string Face { get; set; }

        [JsonProperty("official_verify")]
        public OfficialVerify OfficialVerify { get; set; }

        [JsonProperty("gender")]
        public int Gender { get; set; }
    }

    public class MasterLevel
    {
        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("current")]
        public List<int> Current { get; set; }

        [JsonProperty("next")]
        public List<int> Next { get; set; }
    }

    public class OfficialVerify
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }

    public class RoomNews
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("ctime")]
        public string Ctime { get; set; }

        [JsonProperty("ctime_text")]
        public string CtimeText { get; set; }
    }

    public class BiliBiliGetLiveUserInfoJson
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public GetLiveUserInfo Data { get; set; }
    }
}
