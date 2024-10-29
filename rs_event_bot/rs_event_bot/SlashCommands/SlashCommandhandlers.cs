using Discord;
using Discord.WebSocket;
using Npgsql;
using NpgsqlTypes;

namespace discord_bot_burnerplate_build_1.SlashCommands;

public class SlashCommandhandlers : ISlashCommandhandlers
{
    /// <summary>
    /// Handles the call of /help command when used and displays the corresponding text to the user
    /// </summary>
    /// <param name="command"></param>
    public async Task HandleHelpCommand(SocketSlashCommand command)
    {
        // creates the embed help message
        string helpContents = "```\n" +
                              "/log-rs-run:        Takes in USERNAME, RS LEVEL and POINTS to log the RS run\n" +
                              "/show-leaderboard:  Shows current leaderboard\n" +
                              "/remove-rs-run:     Removes a RS run with a given ID\n" +
                              "```";

        var helpMessage = new EmbedBuilder()
            .WithTitle("Help")
            .WithDescription(helpContents)
            .WithColor(Color.Red);

        await command.RespondAsync(embed: helpMessage.Build(), ephemeral: true);
    }

    /// <summary>
    /// Handles the call of /log-rs-run command when used, displays the added run to the user and adds the input to the database
    /// </summary>
    /// <param name="command"></param>
    /// <param name="connection"></param>
    public async Task HandleLogRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection)
    {
        await command.DeferAsync();

        // takes the input data when /log-rs-run command is called and puts the data into variables
        int rsLevel = Convert.ToInt32(command.Data.Options.First(option => option.Name == "rs-level").Value);
        int points = Convert.ToInt32(command.Data.Options.First(option => option.Name == "points").Value);
        bool isDrs = Convert.ToInt32(command.Data.Options.First(option => option.Name == "star-type").Value) == 1
            ? true
            : false;

        var players = command.Data.Options
            .Where(option => option.Name.StartsWith("player"))
            .Select(option => (SocketUser)option.Value)
            .ToList();
        
        // checks for user duplicates in input
        var uniquePlayers = new HashSet<ulong>();
        
        bool hasDuplicates = false;
        foreach (var player in players)
        {
            if (!uniquePlayers.Add(player.Id))
            {
                hasDuplicates = true;
            }
        }
        
        // handles the bot response if dublicates are found
        if (hasDuplicates)
        {
            var showRepeatedUserError = new EmbedBuilder()
                .WithTitle("RS run wasn't logged")
                .WithDescription("A RS run can't have a single player added to it more than once")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            await command.FollowupAsync(embed: showRepeatedUserError.Build(), ephemeral: true);
            Console.WriteLine("RS run wasn't logged: player duplicated");
            return;
        }
        
        // handles the bot response if points are equal to or below 0
        if (points <= 0)
        {
            var showPointAmountError = new EmbedBuilder()
                .WithTitle("RS run wasn't logged")
                .WithDescription("A RS run can't have 0 or less points")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            await command.FollowupAsync(embed: showPointAmountError.Build(), ephemeral: true);
            Console.WriteLine("RS run wasn't logged: 0 or less points");
            return;
        }
        
        

        // handles adding the players to the database if they weren't registered in it yet
        foreach (var player in players)
        {
            await using (var checkIfUSerExistsCmd =
                         new NpgsqlCommand("SELECT COUNT(1) FROM discorduser WHERE discorduid = @discorduid",
                             connection))
            {
                checkIfUSerExistsCmd.Parameters.AddWithValue("discorduid", NpgsqlTypes.NpgsqlDbType.Bigint,
                    (long)player.Id);
                var exists = (long)await checkIfUSerExistsCmd.ExecuteScalarAsync();

                if (exists == 0)
                {
                    await using (var insertUserCmd = new NpgsqlCommand(
                                     "INSERT INTO discorduser (discorduid, points) VALUES (@discorduid, @points)",
                                     connection))
                    {
                        insertUserCmd.Parameters.AddWithValue("discorduid", NpgsqlTypes.NpgsqlDbType.Bigint,
                            (long)player.Id);
                        insertUserCmd.Parameters.AddWithValue("points", 0f);
                        await insertUserCmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"Added user of UID: {player.Id} to the database");
                }
            }
        }

        // adds the points to each player in the run
        var pointsPerPlayer = Math.Round((float)points / players.Count, 1, MidpointRounding.AwayFromZero);
        foreach (var player in players)
        {
            await using (var addPointsToPlayerCmd = new NpgsqlCommand(
                             "UPDATE discorduser SET points = points + @points WHERE discorduid = @discorduid",
                             connection))
            {
                addPointsToPlayerCmd.Parameters.AddWithValue("discorduid", NpgsqlTypes.NpgsqlDbType.Bigint,
                    (long)player.Id);
                addPointsToPlayerCmd.Parameters.AddWithValue("points", pointsPerPlayer);
                await addPointsToPlayerCmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Added {pointsPerPlayer} points for player {player.Id}");
        }

        // adds the RS Run to the database
        int runIdTemp;
        await using (var getMaxRunIdCmd = new NpgsqlCommand("SELECT COALESCE(MAX(runid), 0) FROM rsrun", connection))
        {
            var maxRunId = (int)await getMaxRunIdCmd.ExecuteScalarAsync();
            var newRunId = maxRunId + 1;
            runIdTemp = newRunId;

            await using (var addRsRunCmd = new NpgsqlCommand(
                             "INSERT INTO rsrun (runid, rslevel, startype, points) VALUES (@runid, @rslevel, @startype, @points)",
                             connection))
            {
                addRsRunCmd.Parameters.AddWithValue("runid", newRunId);
                addRsRunCmd.Parameters.AddWithValue("rslevel", rsLevel);
                addRsRunCmd.Parameters.AddWithValue("startype", isDrs);
                addRsRunCmd.Parameters.AddWithValue("points", points);
                await addRsRunCmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine(
                $"New run with ID: {newRunId}; RsLevel: {rsLevel}; DRS: {isDrs}; Points: {points} has been added to the database");
        }

        // adds a connection between each player in the run to the instance of that run
        foreach (var player in players)
        {
            await using (var addRsRunUserAssociationCmd =
                         new NpgsqlCommand(
                             "INSERT INTO discorduser_rsrun (discorduid, runid) VALUES (@discorduid, @runid)",
                             connection))
            {
                addRsRunUserAssociationCmd.Parameters.AddWithValue("discorduid", NpgsqlTypes.NpgsqlDbType.Bigint,
                    (long)player.Id);
                addRsRunUserAssociationCmd.Parameters.AddWithValue("runid", runIdTemp);
                await addRsRunUserAssociationCmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine(
                $"Added RS run association of user with UID: {player.Id} to run with ID: {runIdTemp} the database");
        }

        string runType = isDrs ? "DRS" : "RS";

        // creates an embed message returned to the user
        string result = $"RS level: {rsLevel}" + Environment.NewLine +
                        $"Run type: {runType}" + Environment.NewLine +
                        $"Points: {points}" + Environment.NewLine +
                        $"Run ID: {runIdTemp}" + Environment.NewLine +
                        "Players:";

        foreach (var player in players)
        {
            result += $"\n- <@{player.Id}>";
        }

        var showLogRsRunMessage = new EmbedBuilder()
            .WithTitle("RS run Logged")
            .WithDescription(result)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        await command.FollowupAsync(embed: showLogRsRunMessage.Build());
    }

    /// <summary>
    /// Handles the call of /show-leaderboard command when used and displays the leaderboard pulled from database to the user
    /// </summary>
    /// <param name="command"></param>
    /// <param name="connection"></param>
    /// <param name="guildId"></param>
    /// <param name="client"></param>
    public async Task HandleShowLeaderboardCommand(SocketSlashCommand command, NpgsqlConnection connection,
    ulong guildId, DiscordSocketClient client)
{
    await command.DeferAsync();

    const int placeWidth = 4;
    const int nicknameWidth = 25;
    const int pointsWidth = 12;
    const int runsWidth = 4;

    // sets the text output format for leaderboard
    string leaderboardOutput = string.Format(
        "{0,-" + placeWidth + "} {1,-" + nicknameWidth + "} {2,-" + pointsWidth + "} {3,-" + runsWidth + "}\n",
        "#", "Nickname", "Points", "Runs"
    );

    int place = 1;
    var guild = client.GetGuild(guildId);

    if (guild == null)
    {
        await command.FollowupAsync("Guild not found.");
        return;
    }

    // goes through database and checks for all users and their associated points
    await using (var getUserStatsCmd =
                 new NpgsqlCommand(
                     "SELECT du.discorduid, du.points, COUNT(dr.discorduid) AS run_count " +
                     "FROM discorduser du " +
                     "LEFT JOIN discorduser_rsrun dr ON du.discorduid = dr.discorduid " +
                     "GROUP BY du.discorduid, du.points " +
                     "ORDER BY du.points DESC;", connection))
    {
        await using (var reader = await getUserStatsCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var discordUid = (ulong)reader.GetInt64(0);
                var points = reader.GetFloat(1);
                var runCount = reader.GetInt64(2);

                var guildUser = guild.GetUser(discordUid);

                string displayName;
                if (guildUser != null)
                {
                    displayName = guildUser.Nickname ?? guildUser.Username;
                }
                else
                {
                    displayName = $"<@{discordUid}>";
                }

                displayName = displayName.Length > nicknameWidth
                    ? displayName.Substring(0, nicknameWidth - 3) + "..."
                    : displayName;

                // adds downloaded data from the database to the text output
                leaderboardOutput += string.Format(
                    "{0,-" + placeWidth + "} {1,-" + nicknameWidth + "} {2,-" + pointsWidth + ":F1} {3,-" +
                    runsWidth + "}\n",
                    place + ".", displayName, points, runCount
                );
                place++;
            }
        }
    }

    // handles cases in which the output would be too long to fit in a singular message
    const int maxMessageLength = 4000; // Discord message character limit for bots (embeds)
    var parts = new List<string>();
    for (int i = 0; i < leaderboardOutput.Length; i += maxMessageLength)
    {
        parts.Add(leaderboardOutput.Substring(i, Math.Min(maxMessageLength, leaderboardOutput.Length - i)));
    }

    for (int partIndex = 0; partIndex < parts.Count; partIndex++)
    {
        var showLeaderboardResults = new EmbedBuilder()
            .WithTitle($"RS Event Leaderboard (Part {partIndex + 1} of {parts.Count})")
            .WithDescription("```" + parts[partIndex] + "```")
            .WithColor(Color.Gold)
            .WithCurrentTimestamp();

        await command.FollowupAsync(embed: showLeaderboardResults.Build());
    }
}

    /// <summary>
    /// Handles the call of /remove-rs-run command when used, displays the information about removed run to the user and removes the run from database
    /// </summary>
    /// <param name="command"></param>
    /// <param name="connection"></param>
    public async Task HandleRemoveRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection)
    {
        await command.DeferAsync();

        // gets run ID from the command input
        int runId = Convert.ToInt32(command.Data.Options.First().Value);

        // checks if the run exists and returns appropriate error message if it does not
        await using (var getRsRunToRemoveCmd =
                     new NpgsqlCommand("SELECT runid FROM rsrun WHERE runid = @runId", connection))
        {
            getRsRunToRemoveCmd.Parameters.AddWithValue("runId", runId);
            var runExists = await getRsRunToRemoveCmd.ExecuteScalarAsync();

            if (runExists == null)
            {
                var showRunDoesntExistError = new EmbedBuilder()
                    .WithTitle("RS run wasn't removed")
                    .WithDescription($"RS run with ID: {runId} doesn't exist")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.FollowupAsync(embed: showRunDoesntExistError.Build(), ephemeral: true);
                return;
            }
        }

        // checks how many points the run was worth
        int pointsForRun = 0;
        await using (var getRunPointsCmd =
                     new NpgsqlCommand("SELECT points FROM rsrun WHERE runid = @runid", connection))
        {
            getRunPointsCmd.Parameters.AddWithValue("runid", runId);
            pointsForRun = (int)(await getRunPointsCmd.ExecuteScalarAsync());
        }

        // gets a list of players associated with the run
        List<ulong> connectedUsers = new List<ulong>();
        await using (var getUsersForRunCmd =
                     new NpgsqlCommand("SELECT discorduid FROM discorduser_rsrun WHERE runid = @runid", connection))
        {
            getUsersForRunCmd.Parameters.AddWithValue("runid", runId);

            await using (var reader = await getUsersForRunCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    connectedUsers.Add((ulong)reader.GetInt64(0));
                }
            }
        }

        // checks how many points should be subtracted from each player
        var pointsToRemove = (float)pointsForRun / connectedUsers.Count;
        foreach (var userId in connectedUsers)
        {
            await using (var removeAssociatedPointsCmd =
                         new NpgsqlCommand(
                             "UPDATE discorduser SET points = points - @pointsToRemove WHERE discorduid = @discorduid",
                             connection))
            {
                removeAssociatedPointsCmd.Parameters.AddWithValue("pointsToRemove", pointsToRemove);
                removeAssociatedPointsCmd.Parameters.AddWithValue("discorduid", NpgsqlTypes.NpgsqlDbType.Bigint,
                    (long)userId);

                await removeAssociatedPointsCmd.ExecuteNonQueryAsync();
            }
        }

        // removes the given run association from the database
        await using (var removeDiscorduserRsRunCmd =
                     new NpgsqlCommand("DELETE FROM discorduser_rsrun WHERE runid = @runId", connection))
        {
            removeDiscorduserRsRunCmd.Parameters.AddWithValue("runId", runId);
            await removeDiscorduserRsRunCmd.ExecuteNonQueryAsync();
        }

        // removes the given run from the database
        await using (var removeRsRunCmd = new NpgsqlCommand("DELETE FROM rsrun WHERE runid = @runId", connection))
        {
            removeRsRunCmd.Parameters.AddWithValue("runId", runId);
            await removeRsRunCmd.ExecuteNonQueryAsync();
        }

        // creates a response message
        string runType = "RS";
        string removalSummary = $"Run ID: {runId}" + Environment.NewLine +
                                $"Run type: {runType}" + Environment.NewLine +
                                $"Points removed from each player: {(float)pointsForRun / connectedUsers.Count}" +
                                Environment.NewLine +
                                "Players:";

        foreach (var userId in connectedUsers)
        {
            removalSummary += $"\n- <@{userId}>";
        }

        var showRemoveRsRunMessage = new EmbedBuilder()
            .WithTitle("RS run Removed")
            .WithDescription(removalSummary)
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        await command.FollowupAsync(embed: showRemoveRsRunMessage.Build());
    }

    /// <summary>
    /// Handles the call of /remove-rs-run-history command when used, removes all data from the database
    /// </summary>
    /// <param name="command"></param>
    /// <param name="connection"></param>
    public async Task HandleRemoveRsRunHistory(SocketSlashCommand command, NpgsqlConnection connection)
    {
        await command.DeferAsync();

        try
        {
            // Removes all data associated with RS events from the database
            await using (var removeDiscordUser_RsRunData = new NpgsqlCommand("DELETE FROM DiscordUser_RsRun WHERE 1 = 1", connection))
            {
                await removeDiscordUser_RsRunData.ExecuteNonQueryAsync();
            }
            
            
            await using (var removeRsRunData = new NpgsqlCommand("DELETE FROM RsRun WHERE 1 = 1", connection))
            {
                await removeRsRunData.ExecuteNonQueryAsync();
            }

            await using (var removeDiscordUserData = new NpgsqlCommand("DELETE FROM DiscordUser WHERE 1 = 1", connection))
            {
                await removeDiscordUserData.ExecuteNonQueryAsync();
            }
            
            // creates a response message
            string removeRsRunHistoryMessage = "All RS runs have been removed";

            var removeRsRunHistoryWarningMessage = new EmbedBuilder()
                .WithTitle("Remove RS Run History")
                .WithDescription(removeRsRunHistoryMessage)
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            await command.FollowupAsync(embed: removeRsRunHistoryWarningMessage.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            // creates an error response message
            string errorMessage = "An error occurred while trying to remove RS runs. Please try again.";

            var removeRsRunHistoryErrorMessage = new EmbedBuilder()
                .WithTitle("Remove RS Run History")
                .WithDescription(errorMessage)
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            
            await command.FollowupAsync(embed: removeRsRunHistoryErrorMessage.Build(), ephemeral: true);
            
            Console.WriteLine($"Error in HandleRemoveRsRunHistory: {ex.Message}");
        }
    }

}