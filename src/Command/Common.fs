namespace MF.GuildWars.Console.Command

open MF.ConsoleApplication

[<RequireQualifiedAccess>]
module CommandError =
    let stringErrors = List.map CommandError.Message >> CommandError.Errors

[<RequireQualifiedAccess>]
module ConsoleApplicationError =
    let stringErrors = CommandError.stringErrors >> ConsoleApplicationError.CommandError
    let commandErrors = CommandError.Errors >> ConsoleApplicationError.CommandError

[<RequireQualifiedAccess>]
module Config =
    open MF.Config
    open MF.ErrorHandling

    let get ((input, output): IO) =
        result {
            let! config =
                input
                |> Input.Option.asString "config"
                |> Option.bind Config.parse
                |> Result.ofOption "Invalid or missing config"
                |> Result.mapError List.singleton

            if output.IsVerbose() then output.Message <| sprintf "Config: %A" config

            return config
        }
