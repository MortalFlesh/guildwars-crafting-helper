namespace MF.GuildWars.Console.Command.CheckCommand

module Checklist =
    open System.Collections.Concurrent
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators
    open MF.Utils
    open MF.Api
    open MF.Storage
    open MF.GuildWars.Console.Command

    let private collectIdsToPrice checklistPrice =
        checklistPrice
        |> Seq.collect (function
            | ItemToPrice.Single item -> [ item.Item.Id ]
            | ItemToPrice.Many items ->
                items
                |> PriceableItemList.getItems
                |> List.choose ItemOrSkipped.getId
        )
        |> Seq.distinct
        |> List.ofSeq

    let private fetchAll (output: MF.ConsoleApplication.Output) itemsCache currencyCache idsCache pricesCache apiKey idsToPrice = asyncResult {
        output.SubTitle "Fetching bank ..."
        let! bankItems =
            GuildWars.fetchBank apiKey
            |> Cache.fetchWithCache output itemsCache "bank"

        output.SubTitle "Fetching inventories ..."
        let! inventoryItems =
            GuildWars.fetchCharactersInventories apiKey
            |> Cache.fetchWithCache output itemsCache "characters"

        output.SubTitle "Fetching trading post delivery ..."
        let! deliveredItems =
            GuildWars.fetchTradingPostDelivery apiKey <@> List.singleton
            |> Cache.fetchWithCache output itemsCache "trading-post"
        let items = bankItems @ inventoryItems @ deliveredItems

        output.SubTitle "Fetching wallet ..."
        let! currencies =
            GuildWars.fetchWallet apiKey <@> List.singleton
            |> Cache.fetchWithCache output currencyCache "wallet"

        output.SubTitle "Fetching known recipes ..."
        let! knownRecipes =
            GuildWars.fetchKnownRecipes apiKey <@> List.singleton
            |> Cache.fetchWithCache output idsCache "recepies"

        output.SubTitle "Fetching item prices ..."
        let! itemPrices =
            idsToPrice
            |> GuildWars.fetchItemPrices <@> List.singleton
            |> Cache.fetchWithCache output pricesCache (sprintf "item-prices-%A" idsToPrice)

        return (items, currencies, knownRecipes, itemPrices)
    }

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

    let prepareChecklist (output: MF.ConsoleApplication.Output) itemsCache currencyCache idsCache pricesCache apiKey (checklist: Checklist): AsyncResult<PreparedChecklist, _> = asyncResult {
        output.Section <| sprintf "Prepare checklist %A" checklist.Name

        let idsToPrice =
            checklist.Price
            |> collectIdsToPrice

        let! items, currencies, knownRecipes, itemPrices =
            idsToPrice
            |> fetchAll output itemsCache currencyCache idsCache pricesCache apiKey

        items
        |> List.length
        |> sprintf "All items: %i"
        |> output.Message

        currencies
        |> List.length
        |> sprintf "All currencies: %i"
        |> output.Message

        let countItemsById id =
            items
            |> List.filter (fun i -> i.Id = id)
            |> List.sumBy (fun i -> i.Count)
        let countItem = countItem countItemsById

        let countedItems =
            checklist.Count
            |> List.map countItem

        let! recipes =
            checklist.Known
            |> List.map (fun recipe -> asyncResult {
                let! recipeId =
                    recipe.Item.Id
                    |> GuildWars.fetchRecipeUnlockId
                    //|> Cache.fetchWithCache output (sprintf "known-recipe-%A" recipe.Item.Id)

                return
                    if knownRecipes |> List.contains recipeId then recipe
                    else { recipe with Value = 0 }
            })
            |> AsyncResult.ofParallelAsyncResults<Recipe, string>
                (sprintf "Error while fetching recepies:\n%A")

        let getPriceById id =
            itemPrices
            |> Map.tryFind id
            |> function
                | Some price -> price
                | _ -> failwithf "Price by id \"%i\" was not found. Check that it is priceable item." id

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

        return {
            Name = checklist.Name
            TabName = TabName checklist.TabName
            Count = countedItems
            Known = recipes
            Price = pricedItems
            IdsToPrice = idsToPrice
            Currency = currencies
        }
    }
