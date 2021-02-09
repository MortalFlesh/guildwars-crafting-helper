// Learn more about F# at http://fsharp.org

open System
open MF.ConsoleStyle
open Environment
open ApiProvider.ChecklistParser
open GuildWarsHelper
open ChecklistWrapper
open Encoder
open Sheets.Api
open ApiProvider

let tableRows data =
        data
        |> List.map Seq.ofList
        |> Seq.ofList

let showLinksForTradingPost idsToPrice =
    Console.section "Prices for checklist (link to trading post)"

    let idsToPrice =
        idsToPrice
        |> Seq.map string
        |> List.ofSeq

    idsToPrice
    |> Seq.length
    |> printfn "Total items to price: %i\n"

    idsToPrice
    |> List.chunkBySize 16
    |> List.iter ((String.concat ",") >> (printfn "https://www.gw2tp.com/custom-list?name=LIST_NAME&ids=%s\n"))

let formatCountedItem = function
    | ItemWithCount.Single i -> sprintf "%s: %i" i.Item.Item.Label i.Count
    | ItemWithCount.Many l ->
        l.Items
        |> List.map (fun i ->
            match i with
            | CountedOrSkippedItem.Skipped s -> sprintf " - %s: skipped" s.Label
            | CountedOrSkippedItem.Counted i -> sprintf " - %s: %i" i.Item.Label i.Count
        )
        |> String.concat "\n"
        |> sprintf "%s:\n%s" l.Label

let formatPricedItem = function
    | ItemWithPrice.Single i -> sprintf "%s: %f" i.Item.Item.Label i.Price
    | ItemWithPrice.Many l ->
        l.Items
        |> List.map (fun i ->
            match i with
            | PricedOrSkippedItem.Skipped s -> sprintf " - %s: skipped" s.Label
            | PricedOrSkippedItem.Priced i -> sprintf " - %s: %f G" i.Item.Label i.Price
        )
        |> String.concat "\n"
        |> sprintf "%s:\n%s" l.Label

let printLines format list =
    list
    |> List.map format
    |> String.concat "\n"
    |> printfn "%s"

[<EntryPoint>]
let main argv =
    Console.title "Guild Wars crafting helper"
    let envs = ".env" |> getEnvs Console.error
    let getEnv = getEnv envs
    let tryGetEnv = tryGetEnv envs
    let log = {
        Section =
            fun section ->
                Console.newLine()
                Console.section section
        HighlightedMessage = Console.subTitle
        Message = Console.message
    }

    let spreadsheetId = "SPREEDSHEET_ID" |> getEnv
    let listName = "LIST_NAME" |> getEnv

    match argv with
    | [| "bank" |] ->
        log.Section "Bank inspection"

        let pricedItems =
            Api.fetchBank ()
            |> List.filter (fun { Count = count } -> count > 0)
            |> List.chunkBySize 100
            |> List.collect (fun items ->
                let itemIds = items |> List.map (fun { Id = id } -> id)
                let prices = itemIds |> Api.fetchItemPrices
                let itemsInfo = itemIds |> Api.fetchItems

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
            )

        log.Section "Encode data"
        pricedItems
        |> encodeBankItems spreadsheetId listName { Letter = "A"; Number = 2 }
        |> writeUpdateData (sprintf "%s/%s" Environment.CurrentDirectory "src/Sheets/data/update.json")
        0
    | _ ->
        let checklist =
            "CHECKLIST"
            |> getEnv
            |> parseChecklist
            |> prepareChecklist log

        checklist.IdsToPrice
        |> showLinksForTradingPost

        log.Section "Count items"
        checklist.Count
        |> printLines formatCountedItem

        log.Section "Known recipes"
        checklist.Known
        |> printLines (fun r -> sprintf "%s: %i" r.Item.Label r.Value)

        log.Section "Price items"
        checklist.Price
        |> printLines formatPricedItem

        log.Section "Wallet items"
        checklist.Currency
        |> printLines (fun c -> sprintf "%s: %i" c.Currency.Label c.Amount)

        log.Section "Encode data"
        checklist
        |> encode spreadsheetId listName
        |> writeUpdateData (sprintf "%s/%s" Environment.CurrentDirectory "src/Sheets/data/update.json")

        0
