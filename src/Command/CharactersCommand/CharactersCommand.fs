namespace MF.GuildWars.Console.Command

[<RequireQualifiedAccess>]
module Characters =
    open System.IO
    open MF.ConsoleApplication
    open MF.Config
    open MF.Api
    open MF.Storage
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators
    open MF.Utils
    open MF.GuildWars.Console.Command
    open MF.GuildWars.Console.Command.CharactersCommand

    [<Literal>]
    let private CharacterLength = 4

    let private logDoneList (output: Output) list =
        list |> List.length |> sprintf " -> <c:green>Done</c> <c:magenta>%A</c> items" |> output.Message |> output.NewLine

    let private updateCharacter (output: MF.ConsoleApplication.Output) config log i (character: Character) = asyncResult {
        let tabName = TabName "Characters"
        let rangeMove = GoogleSheets.rangeMoveBy CharacterLength i
        let letterMove = GoogleSheets.letterMoveBy CharacterLength i

        output.Message <| sprintf "Waiting for %s to start ..." (character.Name |> CharacterName.value)
        do! AsyncResult.sleep (30 * 1000)

        GoogleSheets.clear config tabName ("B3" |> rangeMove) ("D18" |> rangeMove)
        GoogleSheets.clear config tabName ("A22" |> rangeMove) ("D150" |> rangeMove)

        let updateData =
            character
            |> CharactersEncoder.encode config.SpreadsheetId tabName {
                Name = {
                    Letter = "A" |> letterMove
                    Number = 1
                }
                Equipment = {
                    Letter = "B" |> letterMove
                    Number = 3
                }
                Inventory = {
                    Letter = "A" |> letterMove
                    Number = 22
                }
            }

        return!
            updateData
            |> GoogleSheets.updateSheets log config
    }

    let execute: ExecuteCommand = fun (input, output) ->
        asyncResult {
            output.Title "Characters inspection"

            let! config =
                Config.get (input, output)
                |> AsyncResult.ofResult

            let onlyCharacters =
                input |> Input.getArgumentValueAsList "characters"

            output.Section "Fetching character names"
            let! characterNames =
                GuildWars.fetchCharacters config.ApiKey
                >>* logDoneList output

            let selectedCharacterNames =
                match onlyCharacters, characterNames with
                | [], characterNames -> characterNames
                | selected, names -> names |> List.filter (fun (CharacterName name) -> selected |> List.exists name.Contains)

            if output.IsVerbose() then
                selectedCharacterNames
                |> List.map (CharacterName.value >> List.singleton)
                |> output.Options "Character names"

            output.Section "Fetching Characters data"
            let! characters =
                selectedCharacterNames
                |> List.map (GuildWars.fetchCharacter output config.ApiKey GuildWars.BulkMode.Single)
                |> AsyncResult.ofSequentialAsyncResults (sprintf "Fetching character detail failed: %A" >> List.singleton)
                <@> List.concat
                >>* logDoneList output

            characters
            |> List.map Character.format
            |> output.Options "Characters"

            output.Section "Update sheets"
            let log message =
                if output.IsVerbose() then
                    output.Message <| sprintf "[Sheets] %s" message

            do!
                characters
                |> List.sort
                |> List.mapi (updateCharacter output config.GoogleSheets log)
                |> AsyncResult.ofSequentialAsyncResults (sprintf "Updating sheets failed: %A")
                |> AsyncResult.ignore

            return "Done"
        }
        |> Async.RunSynchronously
        |> function
            | Ok message ->
                output.Success message
                ExitCode.Success
            | Error e ->
                e |> List.iter output.Error
                ExitCode.Error
