// Learn more about F# at http://fsharp.org

open System
open MF.ConsoleStyle
open Environment
open ApiProvider
open ApiProvider.ChecklistParser
open ApiProvider.Characters
open ApiProvider

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

    //fetchCharacters()
    //|> tableRows
    //|> Console.table ["Name"]

    let checklist =
        "CHECKLIST"
        |> getEnv
        |> parseChecklist

    checklist.Price
    |> showLinksForTradingPost

    0
