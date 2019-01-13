// Learn more about F# at http://fsharp.org

open System
open MF.ConsoleStyle
open Environment
open ApiProvider
open ApiProvider.ChecklistParser
open ApiProvider.Api
open GuildWarsHelper

let tableRows data =
        data
        |> List.map Seq.ofList
        |> Seq.ofList

let showLinksForTradingPost checklistPrice =
    Console.section "Prices for checklist (link to trading post)"

    let idsToPrice =
        checklistPrice
        |> Seq.collect (function
            | ItemToPrice.Single item -> [item.Item.Id]
            | ItemToPrice.Many items ->
                items
                |> PriceableItemList.getItems
                |> List.choose ItemOrSkipped.getId
        )
        |> Seq.distinct
        |> Seq.map string
        |> List.ofSeq

    idsToPrice
    |> Seq.length
    |> printfn "Total items to price: %i\n"

    idsToPrice
    |> List.chunkBySize 16
    |> List.iter ((String.concat ",") >> (printfn "https://www.gw2tp.com/custom-list?name=LIST_NAME&ids=%s\n"))

let fetchAll () =
    Console.subTitle "Fetching bank ..."
    let bankItems = fetchBank()

    Console.subTitle "Fetching inventories ..."
    let inventoryItems =
        fetchCharacters()
        |> List.collect fetchInventory

    Console.subTitle "Fetching trading post delivery ..."
    let deliveredItems = fetchTradingPostDelivery()
    let items = bankItems @ inventoryItems @ deliveredItems

    Console.subTitle "Fetching wallet ..."
    let currencies = fetchWallet()

    (items, currencies)

let countItem countItemsById = function
    | ItemToCount.Single singleItem ->
        let countedItem: CountedItem = {
            Item = singleItem
            Count = singleItem.Item.Id |> countItemsById
        }
        countedItem |> ItemWithCount.Single
    | ItemToCount.Many itemList ->
        let countedItemList: CountedItemList = {
            Label = itemList.Label
            Cell = itemList.Cell
            Items =
                itemList.Items
                |> List.map (function
                    | ItemOrSkipped.Item item ->
                        {
                            Item = item
                            Count = item.Id |> countItemsById
                        }
                        |> Counted
                    | ItemOrSkipped.Skipped item -> item |> Skipped
                )
        }
        countedItemList |> ItemWithCount.Many

let formatCountedItem = function
    | Single i -> sprintf "%s: %i" i.Item.Item.Label i.Count
    | Many l ->
        l.Items
        |> List.map (fun i ->
            match i with
            | Skipped s -> sprintf " - %s: skipped" s.Label
            | Counted i -> sprintf " - %s: %i" i.Item.Label i.Count
        )
        |> String.concat "\n"
        |> sprintf "%s:\n%s" l.Label

[<EntryPoint>]
let main argv =
    Console.title "Guild Wars crafting helper"
    let envs = ".env" |> getEnvs Console.error
    let getEnv = getEnv envs
    let tryGetEnv = tryGetEnv envs

    let checklist =
        "CHECKLIST"
        |> getEnv
        |> parseChecklist

    checklist.Price
    |> showLinksForTradingPost

    let items, currencies = fetchAll ()

    items
    |> List.length
    |> printfn "All items: %i"

    currencies
    |> List.length
    |> printfn "All currencies: %i"

    Console.newLine ()
    Console.section "Count items"
    let countItemsById id =
        items
        |> List.filter (fun i -> i.Id = id)
        |> List.sumBy (fun i -> i.Count)
    let countItem = countItem countItemsById

    let countedItems =
        checklist.Count
        |> List.map (countItem >> formatCountedItem)

    countedItems
    |> String.concat "\n"
    |> printfn "%s"

    0
