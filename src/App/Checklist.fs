module ChecklistWrapper

open ApiProvider
open ApiProvider.Api
open GuildWarsHelper

let private collectIdsToPrice checklistPrice =
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

let private fetchAll log idsToPrice =
    log.HighlightedMessage "Fetching bank ..."
    let bankItems = fetchBank()

    log.HighlightedMessage "Fetching inventories ..."
    let inventoryItems =
        fetchCharacters()
        |> List.collect fetchInventory

    log.HighlightedMessage "Fetching trading post delivery ..."
    let deliveredItems = fetchTradingPostDelivery()
    let items = bankItems @ inventoryItems @ deliveredItems

    log.HighlightedMessage "Fetching wallet ..."
    let currencies = fetchWallet()

    log.HighlightedMessage "Fetching known recipes ..."
    let knownRecipes = fetchKnownRecipes()

    log.HighlightedMessage "Fetching item prices ..."
    let itemPrices =
        idsToPrice
        |> fetchItemPrices

    (items, currencies, knownRecipes, itemPrices)

let private countItem countItemsById = function
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

let private fetchItemPrices log ids =
    ids
    |> List.length
    |> sprintf "Fetching item prices [%i] ..."
    |> log.HighlightedMessage

    ids
    |> fetchItemPrices

let private priceItem getPriceById = function
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

let prepareChecklist log (checklist: Checklist): PreparedChecklist =
    let idsToPrice =
        checklist.Price
        |> collectIdsToPrice

    let items, currencies, knownRecipes, itemPrices =
        idsToPrice
        |> fetchAll log

    items
    |> List.length
    |> sprintf "All items: %i"
    |> log.Message

    currencies
    |> List.length
    |> sprintf "All currencies: %i"
    |> log.Message

    let countItemsById id =
        items
        |> List.filter (fun i -> i.Id = id)
        |> List.sumBy (fun i -> i.Count)
    let countItem = countItem countItemsById

    let countedItems =
        checklist.Count
        |> List.map countItem

    let recipes =
        checklist.Known
        |> List.map (fun recipe ->
            let recipeId =
                recipe.Item.Id
                |> fetchRecipeUnlockId

            if knownRecipes |> List.contains recipeId then recipe
            else { recipe with Value = 0 }
        )

    let getPriceById id =
        itemPrices
        |> Map.find id

    let pricedItems =
        checklist.Price
        |> List.map (priceItem getPriceById)

    let currencies =
        checklist.Currency
        |> List.map (fun currency ->
            {
                Currency = currency
                Amount =
                    currencies
                    |> List.tryFind (fun c -> c.Id = currency.Id)
                    |> function
                        | Some currency -> currency |> CurrencyItem.getAmount
                        | None -> 0
            }
        )

    {
        Count = countedItems
        Known = recipes
        Price = pricedItems
        IdsToPrice = idsToPrice
        Currency = currencies
    }
