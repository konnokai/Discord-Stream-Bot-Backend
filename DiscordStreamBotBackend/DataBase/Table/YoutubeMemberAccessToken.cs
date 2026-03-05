using System;
using System.ComponentModel.DataAnnotations;

namespace DiscordStreamBotBackend.DataBase.Table;

public partial class YoutubeMemberAccessToken
{
    [Key]
    public ulong DiscordUserId { get; set; }

    public string EncryptedAccessToken { get; set; }

    public DateTime? DateAdded { get; set; }
}
