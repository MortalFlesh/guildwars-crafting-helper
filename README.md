Guild Wars - Crafting helper
============================

## Authorization

### Guild Wars 2 Api
You need to introduce an `Authorization.fs` file with your api key to the **Guild Wars 2** account (_it requires rights for all sources of data, you want to contain in the counting ..._)
```fs
// src/ApiProvider/Authorization.fs
namespace ApiProvider

module internal Authorization =
    [<Literal>]
    let ApiKey = "MY-API-KEY"

```

### Google Sheets Api
You need to create credentials for your google sheets (see https://console.cloud.google.com/apis/api/sheets.googleapis.com/overview).
And place them as `credentials.json` to the `src/Sheets/Wrapper/credentials.json` and then, on the first run, you will be prompted to authorize app by your google account (_link for this will be shown in the terminal_). After that token for authorization will be stored in `src/Sheets/Wrapper/token.json`.

## Configuration

### Env variables
Copy `.env.dist` file as `.env` and fill variables with your data.

### Items to check agains your Guild Wars profile
Copy `.check.dist.json` as `.check.json` and fill all items and currencies you want to check for your crafting.
All items and currencies will be checked against all available sources (_characters inventories, bank, delivered trading post, wallet, ..._) and counted. Then the final count for each item will be set to the cell defined in `.check.json`.

---
## Release
- _todo_

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)
- [FAKE](https://fake.build/fake-gettingstarted.html)

### Build
```bash
fake build
```

### Watch
```bash
fake build target watch
```
