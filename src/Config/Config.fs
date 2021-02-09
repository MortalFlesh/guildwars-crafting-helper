namespace MF.Config

open MF.Api
open MF.Storage

type Config = {
    GuildWars: GuildWars.Search list
    FileStorage: string option
    GoogleSheets: GoogleSheets.Config option
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
                GuildWars =
                    parsed.GuildWars
                    |> Seq.map (fun search ->
                        let search: GuildWars.Search =
                            {
                                Title = search.Title
                                Parameters = search.Param
                            }
                        search
                    )
                    |> Seq.toList

                FileStorage = parsed.FileStorage |> Option.map (fun storage ->
                    storage.File
                )

                GoogleSheets = parsed.GoogleSheets |> Option.map (fun storage ->
                    {
                        Credentials = storage.Credentials
                        Token = storage.Token
                        SpreadsheetId = storage.SpreadsheetId
                        Tab = storage.Tab
                    }
                )
            }
