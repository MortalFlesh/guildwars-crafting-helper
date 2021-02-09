namespace MF.GuildWars.Console.Command

[<RequireQualifiedAccess>]
module CheckCommand =
    open System.IO
    open MF.ConsoleApplication
    open MF.Config
    open MF.Api
    open MF.Storage
    open MF.ErrorHandling
    open MF.Utils

    let execute: ExecuteCommand = fun (input, output) ->

        // GoogleSheets.save "test"
        // failwithf "Done..."
        output.Title "Check items"


        output.Success "Done"
        ExitCode.Success
