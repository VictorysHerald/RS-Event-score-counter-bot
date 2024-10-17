using Discord;
using Discord.WebSocket;
using Npgsql;
using NpgsqlTypes;

namespace discord_bot_burnerplate_build_1.SlashCommands;

public class SlashCommandhandlers : ISlashCommandhandlers
{
    public async Task HandleHelpCommand(SocketSlashCommand command)
    {
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

    public async Task HandleLogRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection)
    {
        await command.DeferAsync();

        int rsLevel = Convert.ToInt32(command.Data.Options.First(option => option.Name == "rs-level").Value);
        int points = Convert.ToInt32(command.Data.Options.First(option => option.Name == "points").Value);
        bool isDrs = Convert.ToInt32(command.Data.Options.First(option => option.Name == "star-type").Value) == 1
            ? true
            : false;

        var players = command.Data.Options
            .Where(option => option.Name.StartsWith("player"))
            .Select(option => (SocketUser)option.Value)
            .ToList();
        
        
        var uniquePlayers = new HashSet<ulong>();
        
        bool hasDuplicates = false;
        foreach (var player in players)
        {
            if (!uniquePlayers.Add(player.Id))
            {
                hasDuplicates = true;
            }
        }

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


    public async Task HandleShowLeaderboardCommand(SocketSlashCommand command, NpgsqlConnection connection,
    ulong guildId, DiscordSocketClient client)
{
    await command.DeferAsync();

    const int placeWidth = 4;
    const int nicknameWidth = 25;
    const int pointsWidth = 12;
    const int runsWidth = 4;

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

                leaderboardOutput += string.Format(
                    "{0,-" + placeWidth + "} {1,-" + nicknameWidth + "} {2,-" + pointsWidth + ":F1} {3,-" +
                    runsWidth + "}\n",
                    place + ".", displayName, points, runCount
                );
                place++;
            }
        }
    }

    const int maxMessageLength = 2000; // Discord message character limit
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


    public async Task HandleRemoveRsRunCommand(SocketSlashCommand command, NpgsqlConnection connection)
    {
        await command.DeferAsync();

        int runId = Convert.ToInt32(command.Data.Options.First().Value);

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

        int pointsForRun = 0;
        await using (var getRunPointsCmd =
                     new NpgsqlCommand("SELECT points FROM rsrun WHERE runid = @runid", connection))
        {
            getRunPointsCmd.Parameters.AddWithValue("runid", runId);
            pointsForRun = (int)(await getRunPointsCmd.ExecuteScalarAsync());
        }

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

        await using (var removeDiscorduserRsRunCmd =
                     new NpgsqlCommand("DELETE FROM discorduser_rsrun WHERE runid = @runId", connection))
        {
            removeDiscorduserRsRunCmd.Parameters.AddWithValue("runId", runId);
            await removeDiscorduserRsRunCmd.ExecuteNonQueryAsync();
        }

        await using (var removeRsRunCmd = new NpgsqlCommand("DELETE FROM rsrun WHERE runid = @runId", connection))
        {
            removeRsRunCmd.Parameters.AddWithValue("runId", runId);
            await removeRsRunCmd.ExecuteNonQueryAsync();
        }

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
}