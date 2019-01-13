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

let collectIdsToPrice checklistPrice =
    checklistPrice
    |> Seq.collect (function
        | ItemToPrice.Single item -> [item.Item.Id]
        | ItemToPrice.Many items ->
            items
            |> PriceableItemList.getItems
            |> List.choose ItemOrSkipped.getId
    )
    |> Seq.distinct
    |> List.ofSeq

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

let fetchAll idsToPrice =
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

    Console.subTitle "Fetching known recipes ..."
    let knownRecipes = fetchKnownRecipes()

    Console.subTitle "Fetching item prices ..."
    let itemPrices =
        idsToPrice
        |> fetchItemPrices

    (items, currencies, knownRecipes, itemPrices)

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
                        |> CountedOrSkippedItem.Counted
                    | ItemOrSkipped.Skipped item ->
                        item |> CountedOrSkippedItem.Skipped
                )
        }
        countedItemList |> ItemWithCount.Many

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

let fetchItemPrices ids =
    Console.subTitlef "Fetching item prices [%i] ..." (ids |> List.length)

    ids
    |> fetchItemPrices

let priceItem getPriceById = function
    | ItemToPrice.Single singleItem ->
        let pricedItem: PricedItem = {
            Item = singleItem
            Price = singleItem.Item.Id |> getPriceById
        }
        pricedItem |> ItemWithPrice.Single
    | ItemToPrice.Many itemList ->
        let pricedItemList: PricedItemList = {
            Label = itemList.Label
            Cell = itemList.Cell
            Items =
                itemList.Items
                |> List.map (function
                    | ItemOrSkipped.Item item ->
                        {
                            Item = item
                            Price = item.Id |> getPriceById
                        }
                        |> PricedOrSkippedItem.Priced
                    | ItemOrSkipped.Skipped item ->
                        item |> PricedOrSkippedItem.Skipped
                )
        }
        pricedItemList |> ItemWithPrice.Many

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

    let checklist =
        "CHECKLIST"
        |> getEnv
        |> parseChecklist

    let idsToPrice =
        checklist.Price
        |> collectIdsToPrice

    idsToPrice
    |> showLinksForTradingPost

    let items, currencies, knownRecipes, itemPrices = fetchAll idsToPrice

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
        |> List.map countItem

    countedItems
    |> printLines formatCountedItem

    Console.newLine ()
    Console.section "Known recipes"
    let recipes =
        checklist.Known
        |> List.map (fun recipe ->
            let recipeId =
                recipe.Item.Id
                |> fetchRecipeUnlockId

            if knownRecipes |> List.contains recipeId then recipe
            else { recipe with Value = 0 }
        )

    recipes
    |> printLines (fun r -> sprintf "%s: %i" r.Item.Label r.Value)

    Console.newLine ()
    Console.section "Price items"
    let getPriceById id =
        itemPrices
        |> Map.find id

    let pricedItem =
        checklist.Price
        |> List.map (priceItem getPriceById)

    pricedItem
    |> printLines formatPricedItem

    0
