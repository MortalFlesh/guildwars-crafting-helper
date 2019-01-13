// Learn more about F# at http://fsharp.org

open System
open MF.ConsoleStyle
open Environment
open ApiProvider
open ApiProvider.ChecklistParser
open ApiProvider.Api

let tableRows data =
        data
        |> List.map Seq.ofList
        |> Seq.ofList

let showLinksForTradingPost checklistPrice =
    Console.section "Prices for checklist (link to trading post)"

    let idsToPrice =
        checklistPrice
        |> Seq.collect (function
            | Single item -> [item.Item.Id]
            | Many items ->
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

    Console.subTitle "Fetching bank..."
    let bankItems =
        fetchBank()

    Console.subTitle "Fetching inventories..."
    let inventoryItems =
        fetchCharacters()
        |> List.collect fetchInventory

    Console.subTitle "Fetching trading post delivery..."
    let deliveredItems =
        fetchTradingPostDelivery()

    let items =
        bankItems
        @ inventoryItems
        @ deliveredItems

    bankItems
    |> List.length
    |> printfn "bank: %A"

    inventoryItems
    |> List.length
    |> printfn "inventories: %A"

    deliveredItems
    |> List.length
    |> printfn "delivered: %A"

    items
    |> List.length
    |> printfn "All items: %A"

    let currency =
        fetchWallet()

    currency
    |> List.length
    |> printfn "All currencies: %A"

    0
