using Discord.WebSocket;
using Npgsql;

namespace discord_bot_burnerplate_build_1.SlashCommands;

public interface ISlashCommandhandlers
{
    public Task HandleHelpCommand(SocketSlashCommand command);
    public Task HandleLogRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection);
    public Task HandleShowLeaderboardCommand(SocketSlashCommand command, NpgsqlConnection connection, ulong guildId, DiscordSocketClient client);
    public Task HandleRemoveRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection);
}