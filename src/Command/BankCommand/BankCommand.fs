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

    let execute: ExecuteCommand = fun (input, output) ->
        asyncResult {
            output.Title "Bank inspection"

            let! config =
                Config.get (input, output)
                |> AsyncResult.ofResult

            output.Section "Fething bank items"
            let! bank =
                config.ApiKey
                |> GuildWars.fetchBank
                >>* (List.length >> sprintf "Fetched %A items" >> output.Success)

            output.Section "Fetching item details"
            let! pricedItems =
                let chunks =
                    bank
                    |> List.filter (fun { Count = count } -> count > 0)
                    |> tee (List.length >> sprintf "<c:dark-yellow>Note:</c> There are <c:magenta>%A</c> items after filter out uncountable items" >> output.Message >> output.NewLine)
                    |> List.chunkBySize 100

                let progress =
                    chunks
                    |> List.length
                    |> output.ProgressStart (chunks |> List.length |> sprintf "Fetching details for %A chunks ...")

                chunks
                |> List.map (fun items -> asyncResult {
                    let itemIds =
                        items
                        |> List.map (fun { Id = id } -> id)

                    let! prices =
                        itemIds
                        |> GuildWars.fetchItemPrices

                    let! itemsInfo =
                        itemIds
                        |> GuildWars.fetchItems

                    return
                        items
                        |> List.map (fun item ->
                            {
                                ItemInfo = itemsInfo.[item.Id]
                                InventoryItem = item
                                Price =
                                    prices
                                    |> Map.tryFind item.Id
                                    |> function
                                        | Some float -> float
                                        | _ -> 0.0
                            }
                        )
                        |> List.filter (fun i -> i.Price > 0.0)
                        |> tee (fun _ -> progress |> output.ProgressAdvance)
                })
                |> AsyncResult.ofSequentialAsyncResults (sprintf "Error while fetching bank item details: %A")
                <!> List.concat
                >>* (fun items ->
                    progress |> output.ProgressFinish
                    items |> List.length |> sprintf "Fetched %A items" |> output.Success
                )

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

            output.Section "Encode data"
            // pricedItems
            // |> encodeBankItems spreadsheetId listName { Letter = "A"; Number = 2 }
            // |> writeUpdateData (sprintf "%s/%s" Environment.CurrentDirectory "src/Sheets/data/update.json")

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
