using discord_bot_burnerplate_build_1.CommandInitialization;
using discord_bot_burnerplate_build_1.SlashCommands;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace discord_bot_burnerplate_build_1;

class Program
{
    private static DiscordSocketClient _client;
    private static ICommandInitialization _commandInitialization;
    private static SlashCommandSwitch _slashCommandSwitch;

    public static async Task Main(string[] args)
    {
        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var builder = new ConfigurationBuilder()
            .AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        var appSettings = new Appsettings();
        configuration.GetSection("AppSettings").Bind(appSettings);

        ulong guildId = appSettings.GuildId;
        string token = appSettings.Token;
        string connectionString = appSettings.ConnectionString;

        _client = new DiscordSocketClient();
        _slashCommandSwitch = new SlashCommandSwitch();
        _commandInitialization = new CommandInitialization.CommandInitialization();

        await using var dbConnection = new NpgsqlConnection(connectionString);
        await dbConnection.OpenAsync();

        _client.Log += Log;
        _client.Ready += async () => await _commandInitialization.Client_Ready(guildId, _client);
        _client.SlashCommandExecuted += async (SocketSlashCommand command) =>
        {
            await _slashCommandSwitch.SlashCommandHandler(command, dbConnection, guildId, _client);
        };

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}