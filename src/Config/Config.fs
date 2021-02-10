namespace MF.Config

open MF.Api
open MF.Storage

type Config = {
    ApiKey: ApiKey
    GoogleSheets: GoogleSheets.Config
}

[<RequireQualifiedAccess>]
module Config =
    open System.IO
    open FSharp.Data

    type private ConfigSchema = JsonProvider<"schema/config.json", SampleIsList = true>

    let parse = function
        | notFound when notFound |> File.Exists |> not -> None
        | file ->
            let parsed =
                file
                |> File.ReadAllText
                |> ConfigSchema.Parse

            Some {
                ApiKey = ApiKey parsed.GuildWars.ApiKey

                GoogleSheets = {
                    Credentials = parsed.GoogleSheets.Credentials
                    Token = parsed.GoogleSheets.Token
                    SpreadsheetId = parsed.GoogleSheets.SpreadsheetId
                    Tab = parsed.GoogleSheets.Tab
                }
            }
