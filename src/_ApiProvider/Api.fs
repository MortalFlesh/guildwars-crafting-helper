namespace ApiProvider

module Api =
    open System
    open FSharp.Data
    open Configuration

    type private CharactersSchema = JsonProvider<const(BaseUrl + "/characters?access_token=" + ApiKey)>
    type private InventorySchema = JsonProvider<"schema/inventory.json">
    type private BankSchema = JsonProvider<const(BaseUrl + "/account/bank?access_token=" + ApiKey)>
    type private Materials = JsonProvider<const(BaseUrl + "/account/materials?access_token=" + ApiKey)>
    type private WalletSchema = JsonProvider<const(BaseUrl + "/account/wallet?access_token=" + ApiKey)>
    type private TradingPostDeliverySchema = JsonProvider<"schema/tradingPostDelivery.json", SampleIsList=true>
    type private TradingPostListingSchema = JsonProvider<"schema/tradingPostListing.json">
    type private ItemsSchema = JsonProvider<"schema/items.json">
    type private KnownRecipeSchema = JsonProvider<const(BaseUrl + "/account/recipes?access_token=" + ApiKey)>
    type private RecipeSchema = JsonProvider<"schema/recipe.json">

    let fetchCharacters () =
        CharactersSchema.GetSamples()
        |> Array.toList

    let fetchInventory character: Inventory =
        let route = BaseUrl + (sprintf "/characters/%s?access_token=" character) + ApiKey

        route
        |> Http.RequestString
        |> InventorySchema.Parse
        |> fun data ->
            data.Bags
            |> Seq.collect (fun bag ->
                bag.Inventory
                |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
            )
        |> List.ofSeq

    let fetchBank () =
        let bankItems =
            BankSchema.GetSamples()
            |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
            |> List.ofSeq
        let materials =
            Materials.GetSamples()
            |> Seq.map (fun material -> { Id = material.Id; Count = material.Count } )
            |> List.ofSeq
        bankItems @ materials

    let fetchWallet () =
        WalletSchema.GetSamples()
        |> Seq.map (fun currency -> { Id = currency.Id; Amount = currency.Value } )
        |> List.ofSeq

    let fetchTradingPostDelivery () =
        let route = BaseUrl + "/commerce/delivery?access_token=" + ApiKey

        route
        |> Http.RequestString
        |> TradingPostDeliverySchema.Parse
        |> fun tradingPost ->
            tradingPost.Items
            |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
        |> List.ofSeq

    let fetchItemPrices (ids: int list) =
        let route =
            ids
            |> List.map string
            |> String.concat ","
            |> sprintf "%s/commerce/listings?ids=%s" BaseUrl

        route
        |> Http.RequestString
        |> TradingPostListingSchema.Parse
        |> Seq.map (fun item ->
            let takeUpTo limit seq =
                let length = seq |> Seq.length
                let realLimit =
                    if length > limit then limit
                    else length

                seq
                |> Seq.take realLimit

            let averagePrice =
                let averageBuys =
                    if item.Buys.Length > 0 then
                        item.Buys
                        |> takeUpTo 3
                        |> List.ofSeq
                        |> List.averageBy (fun i -> i.UnitPrice |> float)
                    else 0.0

                let averageSells =
                    if item.Sells.Length > 0 then
                        item.Sells
                        |> takeUpTo 3
                        |> List.ofSeq
                        |> List.averageBy (fun i -> i.UnitPrice |> float)
                    else 0.0

                [averageBuys; averageSells]
                |> List.average
                |> int
                |> float
                |> fun average -> average / 10000.0 // to gold 12345 -> 1.2345 G
            (item.Id, averagePrice)
        )
        |> Map.ofSeq

    let fetchItems (ids: int list) =
        let route =
            ids
            |> List.map string
            |> String.concat ","
            |> sprintf "%s/items?ids=%s&lang=en" BaseUrl

        route
        |> Http.RequestString
        |> ItemsSchema.Parse
        |> Seq.map (fun item ->

            (
                item.Id,
                {
                    Id = item.Id
                    Name = item.Name
                }
            )
        )
        |> Map.ofSeq

    let fetchKnownRecipes () =
        KnownRecipeSchema.GetSamples()
        |> List.ofArray

    let fetchRecipeUnlockId recipeId =
        let route =
            recipeId
            |> sprintf "%s/items/%i" BaseUrl

        route
        |> Http.RequestString
        |> RecipeSchema.Parse
        |> fun recipe ->
            recipe.Details.RecipeId
