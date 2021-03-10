namespace MF.GuildWars.Console.Command

[<RequireQualifiedAccess>]
module Encode =
    open MF.Api
    open MF.Storage
    open MF.GuildWars.Console.Command

    let encodeItemWithCount data = function
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

    let encodeKnownRecipe data (recipe: Recipe) =
        let cell = recipe.Cell |> Cell.Single |> Cell.value
        let value = [[recipe.Value |> float]]
        (cell, value) :: data

    let encodeItemWithPrice data = function
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

    let encodeCurrency data (currency: CurrencyWithAmount) =
        let cell = currency.Currency.Cell |> Cell.Single |> Cell.value
        let amount = [[currency.Amount |> float]]
        (cell, amount) :: data

    let private floatToString (float: float) =
        float |> string |> (fun s -> s.Replace(".", ","))

    let encodeItemNames data (item: PricedBankItem) =
        [item.ItemInfo.Name] :: data

    let encodeItemCount data (item: PricedBankItem) =
        [item.InventoryItem.Count |> string] :: data

    let encodePricePerPiece data (item: PricedBankItem) =
        [item.Price |> floatToString] :: data

    let encodeTotalPrice data (item: PricedBankItem) =
        [item.Price * (float item.InventoryItem.Count) |> floatToString] :: data
