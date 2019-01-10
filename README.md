Guild Wars - Crafting helper
============================

You need to introduce an `Authorization.fs` file with your api key
```fs
// src/ApiProvider/Authorization.fs
namespace ApiProvider

module internal Authorization =
    [<Literal>]
    let ApiKey = "MY-API-KEY"

```

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
