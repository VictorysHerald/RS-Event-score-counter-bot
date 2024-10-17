using Discord.WebSocket;
using Npgsql;

namespace discord_bot_burnerplate_build_1.SlashCommands
{
    public class SlashCommandSwitch
    {
        private ISlashCommandhandlers _handlers;

        public SlashCommandSwitch()
        {
            _handlers = new SlashCommandhandlers();
        }

        /// <summary>
        /// Handles the calls for all / commands 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="connection"></param>
        /// <param name="guildId"></param>
        /// <param name="client"></param>
        public async Task SlashCommandHandler(SocketSlashCommand command, NpgsqlConnection connection, ulong guildId, DiscordSocketClient client)
        {
            switch (command.Data.Name)
            {
                case "help":
                    await _handlers.HandleHelpCommand(command);
                    break;
                case "log-rs-run":
                    await _handlers.HandleLogRsRunCommand(command, connection);
                    break;
                case "show-leaderboard":
                    await _handlers.HandleShowLeaderboardCommand(command, connection, guildId, client);
                    break;
                case "remove-rs-run":
                    await _handlers.HandleRemoveRsRunCommand(command, connection);
                    break;
            }
        }
    }
}