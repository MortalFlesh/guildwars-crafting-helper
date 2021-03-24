Guild Wars - Crafting helper
============================

> Console application for help a crafting process in Guild Wars 2

## Authorization
Create `.gw2.json` file with
```json
{
    "guildWars": {
        "apiKey": "<api-key>"
    },
    "googleSheets": {
        "credentials": "credentials/credentials.json",
        "token": "credentials/token.json",
        "spreadsheetId": "<spreadsheet-id>"
    }
}
```

### Guild Wars 2 Api
Your api-key should have access for bank, inventories, characters, currencies, ...

### Google Sheets Api
You need to create credentials for your google sheets (see https://console.cloud.google.com/apis/api/sheets.googleapis.com/overview).
And place them as `credentials.json` to the `credentials/credentials.json` and then, on the first run, you will be prompted to authorize app by your google account (_link for this will be shown in the terminal_). After that token for authorization will be stored in `credentials/token.json`.

## Configuration

### Items to check against your Guild Wars profile
Copy `configuration/.check.dist.json` as `configuration/.check.json` and fill all items and currencies you want to check for your crafting.
All items and currencies will be checked against all available sources (_characters inventories, bank, delivered trading post, wallet, ..._) and counted. Then the final count for each item will be set to the cell defined in `.check.json`.

## Console
Download and unzip console from `dist/...`.
Then run it in terminal:

`dist/.../GuildWarsConsole list` will result in:

      _____         _    __     __  _      __                         ___
     / ___/ __ __  (_)  / / ___/ / | | /| / / ___ _  ____  ___       |_  |
    / (_ / / // / / /  / / / _  /  | |/ |/ / / _ `/ / __/ (_-<      / __/
    \___/  \_,_/ /_/  /_/  \_,_/   |__/|__/  \_,_/ /_/   /___/     /____/


    ==================================================================

    Usage:
        command [options] [--] [arguments]

    Options:
        -h, --help            Display this help message
        -q, --quiet           Do not output any message
        -V, --version         Display this application version
        -n, --no-interaction  Do not ask any interactive question
        -v|vv|vvv, --verbose  Increase the verbosity of messages

    Available commands:
        about          Displays information about the current project.
        help           Displays help for a command
        list           Lists commands
    gw
        gw:bank        Inspect a bank for all items and their prices.
        gw:characters  Inspect all characters for equipment and inventories.
        gw:check       Check for resources based on checklist(s).

---
## Release
```bash
./build.sh -t release
```

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)
- [FAKE](https://fake.build/fake-gettingstarted.html)

### Build
```bash
./build.sh
```

### Watch
```bash
./build.sh -t watch
```
