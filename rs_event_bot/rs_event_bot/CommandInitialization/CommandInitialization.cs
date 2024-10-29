using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace discord_bot_burnerplate_build_1.CommandInitialization;

public class CommandInitialization : ICommandInitialization
{
    /// <summary>
    /// Initializes the building process of all / commands
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task Client_Ready(ulong guildId, DiscordSocketClient _client)
    {
        await BuildHelpCommand(guildId, _client);
        await BuildLogRsRunCommand(guildId, _client);
        await BuildShowLeaderboardCommand(guildId, _client);
        await BuildRemoveRsRunCommand(guildId, _client);
        await BuildRemoveRsRunHistoryCommand(guildId, _client);
    }

    /// <summary>
    /// Builds the /help command
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task BuildHelpCommand(ulong guildId, DiscordSocketClient _client)
    {
        var helpGuildCommand = new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Displays help message");
        try
        {
            await _client.Rest.CreateGuildCommand(helpGuildCommand.Build(), guildId);
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    
    /// <summary>
    /// Builds the /log-rs-run command
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task BuildLogRsRunCommand(ulong guildId, DiscordSocketClient _client)
    {
        var logRsRunCommand = new SlashCommandBuilder()
            .WithName("log-rs-run")
            .WithDescription("Logs RS run results")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("rs-level")
                .WithDescription("RS run level")
                .WithType(ApplicationCommandOptionType.Integer)
                .WithRequired(true)
                .AddChoice("2", 2)
                .AddChoice("3", 3)
                .AddChoice("4", 4)
                .AddChoice("5", 5)
                .AddChoice("6", 6)
                .AddChoice("7", 7)
                .AddChoice("8", 8)
                .AddChoice("9", 9)
                .AddChoice("10", 10)
                .AddChoice("11", 11)
                .AddChoice("12", 12))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("star-type")
                .WithDescription("Star type")
                .WithType(ApplicationCommandOptionType.Integer)
                .WithRequired(true)
                .AddChoice("RS", 0)
                .AddChoice("DRS", 1))
            .AddOption("points", ApplicationCommandOptionType.Integer, "Points obtained", isRequired: true)
            .AddOption("player1", ApplicationCommandOptionType.User, "Select the player who did the RS run",
                isRequired: true)
            .AddOption("player2", ApplicationCommandOptionType.User, "Additional player (optional)", isRequired: false)
            .AddOption("player3", ApplicationCommandOptionType.User, "Additional player (optional)", isRequired: false)
            .AddOption("player4", ApplicationCommandOptionType.User, "Additional player (optional)", isRequired: false);

        try
        {
            await _client.Rest.CreateGuildCommand(logRsRunCommand.Build(), guildId);
        }
        catch (ApplicationCommandException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    /// <summary>
    /// Builds the /show-leaderboard command
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task BuildShowLeaderboardCommand(ulong guildId, DiscordSocketClient _client)
    {
        var showRsRunsCommand = new SlashCommandBuilder()
            .WithName("show-leaderboard")
            .WithDescription("Shows RS leaderboard");
        try
        {
            await _client.Rest.CreateGuildCommand(showRsRunsCommand.Build(), guildId);
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    /// <summary>
    /// Builds the /remove-rs-run command
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task BuildRemoveRsRunCommand(ulong guildId, DiscordSocketClient _client)
    {
        var removeRsRunCommand = new SlashCommandBuilder()
            .WithName("remove-rs-run")
            .WithDescription("Removes a logged RS run")
            .AddOption("run-id", ApplicationCommandOptionType.Integer, "Run ID", isRequired: true);

        try
        {
            await _client.Rest.CreateGuildCommand(removeRsRunCommand.Build(), guildId);
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    /// <summary>
    /// Builds the /remove-rs-run-history command
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="_client"></param>
    public async Task BuildRemoveRsRunHistoryCommand(ulong guildId, DiscordSocketClient _client)
    {
        var removeRsRunHistoryCommand = new SlashCommandBuilder()
            .WithName("remove-rs-run-history")
            .WithDescription("removes a history of all RS runs");

        try
        {
            await _client.Rest.CreateGuildCommand(removeRsRunHistoryCommand.Build(), guildId);
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
}