using System;
using System.ComponentModel.DataAnnotations;

namespace DiscordStreamBotBackend.DataBase.Table;

public partial class YoutubeChannelSpider
{
    [Key]
    public string ChannelId { get; set; }

    public string ChannelTitle { get; set; }

    public ulong GuildId { get; set; }

    public bool IsTrustedChannel { get; set; }

    public DateTime LastSubscribeTime { get; set; }

    public DateTime? DateAdded { get; set; }
}
