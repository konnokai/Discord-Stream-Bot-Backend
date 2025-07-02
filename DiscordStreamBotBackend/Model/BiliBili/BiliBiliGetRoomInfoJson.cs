using Newtonsoft.Json;
using System.Collections.Generic;

namespace DiscordStreamBotBackend.Model.BiliBili
{// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Badge
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }

    public class GetRoomInfo
    {
        [JsonProperty("uid")]
        public int Uid { get; set; }

        [JsonProperty("room_id")]
        public int RoomId { get; set; }

        [JsonProperty("short_id")]
        public int ShortId { get; set; }

        [JsonProperty("attention")]
        public int Attention { get; set; }

        [JsonProperty("online")]
        public int Online { get; set; }

        [JsonProperty("is_portrait")]
        public bool IsPortrait { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("live_status")]
        public int LiveStatus { get; set; }

        [JsonProperty("area_id")]
        public int AreaId { get; set; }

        [JsonProperty("parent_area_id")]
        public int ParentAreaId { get; set; }

        [JsonProperty("parent_area_name")]
        public string ParentAreaName { get; set; }

        [JsonProperty("old_area_id")]
        public int OldAreaId { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("user_cover")]
        public string UserCover { get; set; }

        [JsonProperty("keyframe")]
        public string Keyframe { get; set; }

        [JsonProperty("is_strict_room")]
        public bool IsStrictRoom { get; set; }

        [JsonProperty("live_time")]
        public string LiveTime { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("is_anchor")]
        public int IsAnchor { get; set; }

        [JsonProperty("room_silent_type")]
        public string RoomSilentType { get; set; }

        [JsonProperty("room_silent_level")]
        public int RoomSilentLevel { get; set; }

        [JsonProperty("room_silent_second")]
        public int RoomSilentSecond { get; set; }

        [JsonProperty("area_name")]
        public string AreaName { get; set; }

        [JsonProperty("pendants")]
        public string Pendants { get; set; }

        [JsonProperty("area_pendants")]
        public string AreaPendants { get; set; }

        [JsonProperty("hot_words")]
        public List<string> HotWords { get; set; }

        [JsonProperty("hot_words_status")]
        public int HotWordsStatus { get; set; }

        [JsonProperty("verify")]
        public string Verify { get; set; }

        [JsonProperty("new_pendants")]
        public NewPendants NewPendants { get; set; }

        [JsonProperty("up_session")]
        public string UpSession { get; set; }

        [JsonProperty("pk_status")]
        public int PkStatus { get; set; }

        [JsonProperty("pk_id")]
        public int PkId { get; set; }

        [JsonProperty("battle_id")]
        public int BattleId { get; set; }

        [JsonProperty("allow_change_area_time")]
        public int AllowChangeAreaTime { get; set; }

        [JsonProperty("allow_upload_cover_time")]
        public int AllowUploadCoverTime { get; set; }

        [JsonProperty("studio_info")]
        public StudioInfo StudioInfo { get; set; }
    }

    public class Frame
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("area")]
        public int Area { get; set; }

        [JsonProperty("area_old")]
        public int AreaOld { get; set; }

        [JsonProperty("bg_color")]
        public string BgColor { get; set; }

        [JsonProperty("bg_pic")]
        public string BgPic { get; set; }

        [JsonProperty("use_old_area")]
        public bool UseOldArea { get; set; }
    }

    public class MobileFrame
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("area")]
        public int Area { get; set; }

        [JsonProperty("area_old")]
        public int AreaOld { get; set; }

        [JsonProperty("bg_color")]
        public string BgColor { get; set; }

        [JsonProperty("bg_pic")]
        public string BgPic { get; set; }

        [JsonProperty("use_old_area")]
        public bool UseOldArea { get; set; }
    }

    public class NewPendants
    {
        [JsonProperty("frame")]
        public Frame Frame { get; set; }

        [JsonProperty("badge")]
        public Badge Badge { get; set; }

        [JsonProperty("mobile_frame")]
        public MobileFrame MobileFrame { get; set; }

        [JsonProperty("mobile_badge")]
        public object MobileBadge { get; set; }
    }

    public class BiliBiliGetRoomInfoJson
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public GetRoomInfo Data { get; set; }
    }

    public class StudioInfo
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("master_list")]
        public List<object> MasterList { get; set; }
    }


}
