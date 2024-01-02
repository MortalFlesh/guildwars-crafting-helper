namespace MF.Storage

[<RequireQualifiedAccess>]
module GoogleSheets =
    open MF.ErrorHandling

    open Google.Apis.Auth.OAuth2
    open Google.Apis.Sheets.v4
    open Google.Apis.Sheets.v4.Data
    open Google.Apis.Services
    open Google.Apis.Util.Store
    open System
    open System.Collections.Generic
    open System.IO
    open System.Threading

    type Config = {
        Credentials: string
        Token: string
        SpreadsheetId: string
    }

    let private letters =
        let baseLetters = [ "A"; "B"; "C"; "D"; "E"; "F"; "G"; "H"; "I"; "J"; "K"; "L"; "M"; "N"; "O"; "P"; "Q"; "R"; "S"; "T"; "U"; "V"; "W"; "X"; "Y"; "Z" ]

        [
            yield! baseLetters
            yield! baseLetters |> List.map ((+) "A")
        ]

    let letter i =
        if i > letters.Length then failwithf "[Sheets] Letter index %A is out of bound." i
        letters.[i]

    let letterNumber letter =
        match letters |> List.tryFindIndex ((=) letter) with
        | Some i -> i
        | _ -> failwithf "[Sheets] Letter %A is out of bound." letter

    let letterMoveBy length i (cellLetter: string) =
        if i <= 0 then cellLetter
        else
            let letterNumber = cellLetter |> letterNumber
            letterNumber + i * length |> letter

    let rangeMoveBy length i (range: string) =
        if i <= 0 then range
        else
            let letter = string range.[0] |> letterMoveBy length i
            let number = int range.[1..]
            sprintf "%s%d" letter number

    let private createClient config =
        let scopes = [ SheetsService.Scope.Spreadsheets ]
        let applicationName = "GuildWarsChecker"

        use stream = File.OpenRead(config.Credentials)
        let credentials =
            GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                FileDataStore(config.Token, true)
            ).Result

        credentials.RefreshTokenAsync(CancellationToken.None)
        |> Async.AwaitTask
        |> Async.map ignore
        |> Async.Start

        new SheetsService(
            BaseClientService.Initializer(
                HttpClientInitializer = credentials,
                ApplicationName = applicationName
            )
        )

    let private range (TabName tab) fromCell toCell =
        sprintf "%s!%s:%s" tab fromCell toCell

    let private range2 tab (fromLetter, fromNumber) (toLetter, toNumber) =
        range tab (sprintf "%s%d" fromLetter fromNumber) (sprintf "%s%d" toLetter toNumber)

    /// Helper function to convert F# list to C# List
    let private data<'a> (values: 'a list): List<'a> =
        values |> ResizeArray<'a>

    /// Helper function to convert F# list to C# IList
    let private idata<'a> (values: 'a list): IList<'a> =
        values |> data :> IList<'a>

    let private valuesRangeStatic tab fromCell toCell (values: _ list list) =
        let toObj a = a :> obj

        let values =
            values
            |> List.map (List.map toObj >> idata)
            |> data

        ValueRange (
            Range = range tab fromCell toCell,
            Values = values
        )

    let private valuesRange tab (startLetter, startNumber) (values: _ list list) =
        let toLetter =
            let toLetterIndex =
                values
                |> List.map List.length
                |> List.sortDescending
                |> List.head

            letters.[toLetterIndex - 1]

        let toNumber = startNumber + values.Length - 1

        values
        |> valuesRangeStatic tab (sprintf "%s%d" startLetter startNumber) (sprintf "%s%d" toLetter toNumber)

    let private logError context (e: exn) =
        eprintfn "[GoogleSheets][%s] %s\n%A" context e.Message e

    /// From "D2:D27" to ("D", 2)
    let private rangeStartFromString (range: string) =
        let range =
            match range.Split ":" with
            | [| range; _  |] -> range
            | _ -> failwithf "Invalid format of range %A" range

        let letter = range.[0] |> string
        let number = range.[1..] |> int

        letter, number

    let private batchUpdateSheets (client: SheetsService) spreadsheetId (valuesRange: ValueRange) =
        let requestBody =
            BatchUpdateValuesRequest(
                ValueInputOption = "USER_ENTERED",
                Data = data [ valuesRange ]
            )

        let request = client.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetId)

        request.Execute() |> ignore

    let private saveItems config (serialize: _ -> string) tabName = function
        | [] -> ()
        | items ->
            try
                let valuesRange =
                    items
                    |> List.choose (fun item ->
                        let row = serialize item

                        match row.Split ";" |> Seq.toList with
                        | [] -> None
                        | values -> Some values
                    )
                    |> valuesRange tabName ("A", 2)

                use service = createClient config

                valuesRange |> batchUpdateSheets service config.SpreadsheetId

            with e -> e |> logError "Save"

    let private loadItems config parse tab () =
        try
            use service = createClient config

            let request = service.Spreadsheets.Values.Get(config.SpreadsheetId, range tab "A2" "M100")
            let response = request.Execute()
            let values = response.Values

            if values |> Seq.isEmpty then []
            else
                values
                |> Seq.map (fun row ->
                    row
                    |> Seq.map (fun i -> i.ToString())
                    |> String.concat ";"
                )
                |> Seq.choose parse
                |> Seq.toList

        with e ->
            e |> logError "Load"
            []

    let clear config tab fromCell toCell =
        try
            use service = createClient config

            let request = service.Spreadsheets.Values.Clear(ClearValuesRequest(), config.SpreadsheetId, range tab fromCell toCell)
            request.Execute() |> ignore

        with e -> e |> logError "Clear"

    let updateSheets log config (updateData: UpdateData) = asyncResult {
        use client = createClient config

        let updateSheetsByData spreadsheetId listName data =
            data
            |> List.iter (fun (range: string, values) ->
                log <| sprintf "Update range %A ..." range

                let fromCell, toCell =
                    match range.Split ":" with
                    | [| cell |] -> cell, cell
                    | [| fromCell; toCell |] -> fromCell, toCell
                    | _ -> failwithf "Invalid range %A - expected \"From:To\"" range

                values
                |> valuesRangeStatic listName fromCell toCell
                |> batchUpdateSheets client spreadsheetId
            )

        match updateData with
        | UpdateData.String { SpreadsheetId = spreadsheetId; ListName = listName; Data = data } ->
            data
            |> updateSheetsByData spreadsheetId listName

        | UpdateData.Float { SpreadsheetId = spreadsheetId; ListName = listName; Data = data } ->
            data
            |> updateSheetsByData spreadsheetId listName

        return ()
    }
