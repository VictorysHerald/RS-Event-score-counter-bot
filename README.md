# RS Event Bot

### Description
This bot was created in order to work on a single server at a time as a simple and quick solution for anyone who wants to have a way of logging their members' scores and doesn't want to create a new bot from the ground up. It is meant to be a simple solution with basic functionalities to support oranization of RS Events on your own Discord server<br>


### Getting Started

To run the bot, you will need to provide an `appsettings.json` file in the following location:<br>
\rs_event_bot\rs_event_bot\bin\Debug\net8.0<br>
This file should contain your server (guild) ID, bot token, and PostgreSQL database connection details.<br>

### `appsettings.json` Structure:
```json
{
  "appsettings": {
    "guildId": 123456789012345678,
    "token": "YOUR_BOT_TOKEN_HERE",
    "connectionString": "Host=your_host;Port=your_port;Username=your_username;Password=your_password;Database=your_database"
  }
}
```
guildId: The ID of the Discord server (guild) where the bot will operate.<br>
token: The bot token provided by the Discord developer portal.<br>
connectionString: Your PostgreSQL database connection details.<br>

### Database Setup
The bot uses a PostgreSQL database to track Discord users' points and RS event runs. To set up the necessary tables, use the following SQL commands to create them in your PostgreSQL instance:

```sql
CREATE TABLE DiscordUser
(
    DiscordUid BIGINT NOT NULL PRIMARY KEY,
    Points FLOAT NOT NULL
);

CREATE TABLE RsRun
(
    RunId INT NOT NULL PRIMARY KEY,
    RsLevel INT NOT NULL,
    StarType BOOLEAN NOT NULL,
    Points INT NOT NULL
);

CREATE TABLE DiscordUser_RsRun
(
    DiscordUid BIGINT NOT NULL,
    RunId INT NOT NULL,
    PRIMARY KEY (DiscordUid, RunId),
    FOREIGN KEY (DiscordUid) REFERENCES DiscordUser(DiscordUid),
    FOREIGN KEY (RunId) REFERENCES RsRun(RunId)
);
```

### Necessary NuGet packages to run the bot (ensure they are installed in the project):<br>
Discord.Net (3.16.0)<br>
Microsoft.Extensions.Configuration (8.0.0)<br>
Microsoft.Extensions.Configuration.Json (8.0.1)<br>
Microsoft.Extensions.Options.ConfigurationExtensions (8.0.0)<br>
Npgsql (8.0.5)<br>
