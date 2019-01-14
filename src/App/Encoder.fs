module Encoder

open ApiProvider
open GuildWarsHelper
open Sheets

let private encodeItemWithCount data = function
    | ItemWithCount.Single i ->
        let cell = i.Item.Cell |> Cell.Single |> Cell.value
        let count = [[i.Count |> float]]
        (cell, count) :: data
    | ItemWithCount.Many l ->
        let cell = l.Cell |> Cell.Range |> Cell.value
        let counts =
            l.Items
            |> List.map (function
                | CountedOrSkippedItem.Skipped _ -> [0.0]
                | CountedOrSkippedItem.Counted i -> [i.Count |> float]
            )
        (cell, counts) :: data

let private encodeKnownRecipe data (recipe: Recipe) =
    let cell = recipe.Cell |> Cell.Single |> Cell.value
    let value = [[recipe.Value |> float]]
    (cell, value) :: data

let private encodeItemWithPrice data = function
    | ItemWithPrice.Single i ->
        let cell = i.Item.Cell |> Cell.Single |> Cell.value
        let count = [[i.Price]]
        (cell, count) :: data
    | ItemWithPrice.Many l ->
        let cell = l.Cell |> Cell.Range |> Cell.value
        let prices =
            l.Items
            |> List.map (function
                | PricedOrSkippedItem.Skipped _ -> [0.0]
                | PricedOrSkippedItem.Priced i -> [i.Price]
            )
        (cell, prices) :: data

let private encodeCurrency data (currency: CurrencyWithAmount) =
    let cell = currency.Currency.Cell |> Cell.Single |> Cell.value
    let amount = [[currency.Amount |> float]]
    (cell, amount) :: data

let encode spreadsheetId listName preparedChecklist =
    let data = []
    let data =
        preparedChecklist.Count
        |> List.fold encodeItemWithCount data
    let data =
        preparedChecklist.Price
        |> List.fold encodeItemWithPrice data
    let data =
        preparedChecklist.Known
        |> List.fold encodeKnownRecipe data
    let data =
        preparedChecklist.Currency
        |> List.fold encodeCurrency data

    {
        spreadsheetId = spreadsheetId
        listName = listName
        data = data |> List.rev
    }
