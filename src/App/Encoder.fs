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

    UpdateData.Float {
        spreadsheetId = spreadsheetId
        listName = listName
        data = data |> List.rev
    }

let private floatToString (float: float) =
    float |> string |> (fun s -> s.Replace(".", ","))

let private encodeItemNames data (item: PricedBankItem) =
    [item.ItemInfo.Name] :: data

let private encodeItemCount data (item: PricedBankItem) =
    [item.InventoryItem.Count |> string] :: data

let private encodePricePerPiece data (item: PricedBankItem) =
    [item.Price |> floatToString] :: data

let private encodeTotalPrice data (item: PricedBankItem) =
    [item.Price * (float item.InventoryItem.Count) |> floatToString] :: data

let encodeBankItems spreadsheetId listName startingCell pricedItems =
    let data = []
    let totalItems = pricedItems |> List.length

    // A2 ... A100 -> i.names
    let cell = startingCell
    let nameRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
    let nameData =
        pricedItems
        |> List.fold encodeItemNames []
    let data = (nameRange, nameData) :: data

    // B2 ... B100 -> i.count
    let cell = { startingCell with Letter = "B" }
    let countRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
    let countData =
        pricedItems
        |> List.fold encodeItemCount []
    let data = (countRange, countData) :: data

    // C2 ... C100 -> i.pricePerPiece
    let cell = { startingCell with Letter = "C" }
    let pricePerPieceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
    let pricePerPieceData =
        pricedItems
        |> List.fold encodePricePerPiece []
    let data = (pricePerPieceRange, pricePerPieceData) :: data

    // D2 ... D100 -> i.price * count
    let cell = { cell with Letter = "D" }
    let totalPriceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
    let totalPriceData =
        pricedItems
        |> List.fold encodeTotalPrice []
    let data = (totalPriceRange, totalPriceData) :: data

    UpdateData.String {
        spreadsheetId = spreadsheetId
        listName = listName
        data = data |> List.rev
    }
