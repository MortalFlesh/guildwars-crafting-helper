namespace MF.GuildWars.Console.Command

[<RequireQualifiedAccess>]
module Bank =
    open System.IO
    open MF.ConsoleApplication
    open MF.Config
    open MF.Api
    open MF.Storage
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators
    open MF.Utils
    open MF.GuildWars.Console.Command
    open MF.GuildWars.Console.Command.BankCommand

    let execute = ExecuteAsyncResult <| fun (input, output) ->
        asyncResult {
            output.Title "Bank inspection"

            let! config = Config.get (input, output)

            output.Section "Fetching bank items"
            let! (bank: Inventory) =
                config.ApiKey
                |> GuildWars.fetchBank
                >>* (List.length >> sprintf "Fetched %A items" >> output.Success)

            output.Section "Fetching item details"
            let! (pricedItems: ItemWithInfoAndPrice list) =
                bank
                |> GuildWars.fetchPricedItems output GuildWars.BulkMode.Single

            pricedItems
            |> List.sortByDescending (fun item -> item.Price)
            |> List.take 10
            |> List.map (fun item ->
                [
                    sprintf "id: <c:gray>%A</c>" item.InventoryItem.Id
                    sprintf "name: <c:cyan>%s</c>" item.ItemInfo.Name
                    sprintf "count: <c:magenta>%A</c>" item.InventoryItem.Count
                    sprintf "price: <c:yellow>%A</c>" item.Price
                    sprintf "total price: <c:yellow>%A</c>" (item.Price * float item.InventoryItem.Count)
                ]
            )
            |> output.Options "Bank items (top 10 most expensive items)"

            output.Section "Fetching Wallet"
            let! wallet =
                config.ApiKey
                |> GuildWars.fetchWallet <@> List.singleton
                >>* (List.length >> sprintf "Fetched %A currencies" >> output.Success)

            output.Section "Update sheets"
            GoogleSheets.clear config.GoogleSheets (TabName "Bank") "A2" "D400"

            let log message =
                if output.IsVerbose() then
                    output.Message <| sprintf "[Sheets] %s" message

            do!
                pricedItems
                |> BankEncoder.encodeItems config.GoogleSheets.SpreadsheetId (TabName "Bank") { Letter = "A"; Number = 2 }
                |> GoogleSheets.updateSheets log config.GoogleSheets

            do!
                wallet
                |> BankEncoder.encodeCurrencies config.GoogleSheets.SpreadsheetId (TabName "Bank") { Letter = "I"; Number = 2 }
                |> GoogleSheets.updateSheets log config.GoogleSheets

            return ExitCode.Success
        }
        |> AsyncResult.mapError ConsoleApplicationError.stringErrors
