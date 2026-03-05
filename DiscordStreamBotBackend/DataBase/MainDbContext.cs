using DiscordStreamBotBackend.DataBase.Table;
using Microsoft.EntityFrameworkCore;

namespace DiscordStreamBotBackend.DataBase;

public partial class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options)
{
    public virtual DbSet<YoutubeChannelSpider> YoutubeChannelSpider { get; set; }
    public virtual DbSet<YoutubeMemberAccessToken> YoutubeMemberAccessToken { get; set; }
    public virtual DbSet<YoutubeMemberCheck> YoutubeMemberCheck { get; set; }
}
