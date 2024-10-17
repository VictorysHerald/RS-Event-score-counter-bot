# RS Event Bot

### Getting Started

To run the bot, you will need to provide an `appsettings.json` file in the following location:\n
\rs_event_bot\rs_event_bot\bin\Debug\net8.0\n
This file should contain your server (guild) ID, bot token, and PostgreSQL database connection details.\n

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
guildId: The ID of the Discord server (guild) where the bot will operate.
token: The bot token provided by the Discord developer portal.
connectionString: Your PostgreSQL database connection details.

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

### Necessary NuGet packages to run the bot (ensure they are installed in the project):
Discord.Net (3.16.0)
Microsoft.Extensions.Configuration (8.0.0)
Microsoft.Extensions.Configuration.Json (8.0.1)
Microsoft.Extensions.Options.ConfigurationExtensions (8.0.0)
Npgsql (8.0.5)
