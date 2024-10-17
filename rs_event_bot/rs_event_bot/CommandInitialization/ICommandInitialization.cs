using Discord.WebSocket;

namespace discord_bot_burnerplate_build_1.CommandInitialization;

public interface ICommandInitialization
{
    public Task Client_Ready(ulong guildId, DiscordSocketClient _client);
    public Task BuildHelpCommand(ulong guildId, DiscordSocketClient _client);
    public Task BuildLogRsRunCommand(ulong guildId, DiscordSocketClient _client);
    public Task BuildShowLeaderboardCommand(ulong guildId, DiscordSocketClient _client);
    public Task BuildRemoveRsRunCommand(ulong guildId, DiscordSocketClient _client);
    public Task BuildRemoveRsRunHistory(ulong guildId, DiscordSocketClient _client);
}