namespace MF.GuildWars.Console.Command.CharactersCommand

open MF.Api

[<RequireQualifiedAccess>]
module Character =
    let format character =
        [
            character.Name |> CharacterName.value |> sprintf "<c:cyan>%s</c>"
            character.Inventory |> List.length |> sprintf "<c:gray>Inventory.Bags</c>[<c:magenta>%A</c>]"
            character.Equipment |> Equipment.count |> sprintf "<c:yellow>Equipment</c>[<c:magenta>%A</c>]"
        ]
