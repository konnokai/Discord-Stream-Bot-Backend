namespace Discord_Stream_Bot_Backend
{
    public class DiscordUser
    {
        public string id { get; set; }
        public string username { get; set; }
        public string avatar { get; set; }
        public object avatar_decoration { get; set; }
        public string discriminator { get; set; }
        public int public_flags { get; set; }
        public int flags { get; set; }
        public string banner { get; set; }
        public object banner_color { get; set; }
        public object accent_color { get; set; }
        public string locale { get; set; }
        public bool mfa_enabled { get; set; }
        public int premium_type { get; set; }
        public string email { get; set; }
        public bool verified { get; set; }
    }
}
