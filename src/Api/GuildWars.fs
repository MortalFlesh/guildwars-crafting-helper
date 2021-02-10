namespace MF.Api

[<RequireQualifiedAccess>]
module GuildWars =
    open System
    open FSharp.Data
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators

    type private CharactersSchema = JsonProvider<"schema/characters.json">
    type private InventorySchema = JsonProvider<"schema/inventory.json">
    type private BankSchema = JsonProvider<"schema/bank.json">
    type private MaterialsSchema = JsonProvider<"schema/materials.json">
    type private WalletSchema = JsonProvider<"schema/wallet.json">
    type private TradingPostDeliverySchema = JsonProvider<"schema/tradingPostDelivery.json", SampleIsList=true>
    type private TradingPostListingSchema = JsonProvider<"schema/tradingPostListing.json">
    type private ItemsSchema = JsonProvider<"schema/items.json">
    type private KnownRecipeSchema = JsonProvider<"schema/knownRecepies.json">
    type private RecipeSchema = JsonProvider<"schema/recipe.json">

    [<RequireQualifiedAccess>]
    module private Api =
        [<Literal>]
        let BaseUrl = "https://api.guildwars2.com/v2"

        let path (ApiKey token) path =
            sprintf "%s/%s?access_token=%s" BaseUrl path token

        let fetch url =
            url
            |> Http.AsyncRequestString
            |> AsyncResult.ofAsyncCatch (sprintf "Error while fetching %A:\n%A" url)

    let fetchInventory apiKey character: AsyncResult<Inventory, _> = asyncResult {
        let! response =
            sprintf "characters/%s" character
            |> Api.path apiKey
            |> Api.fetch

        let data =
            response
            |> InventorySchema.Parse

        return
            data.Bags
            |> Seq.collect (fun bag ->
                bag.Inventory
                |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
            )
            |> List.ofSeq
    }

    let fetchCharacters apiKey: AsyncResult<Inventory, string list> = asyncResult {
        let! response =
            "characters"
            |> Api.path apiKey
            |> Api.fetch <@> List.singleton

        let characters =
            response
            |> CharactersSchema.Parse

        return!
            characters
            |> Seq.toList
            |> List.map (fetchInventory apiKey)
            |> AsyncResult.ofParallelAsyncResults (sprintf "Fetching inventories failed:\n%A") <!> List.concat
    }

    let fetchBank apiKey =
        let fetchBankItems =
            "account/bank"
            |> Api.path apiKey
            |> Api.fetch
            <!> fun response ->
                response
                |> BankSchema.Parse
                |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
                |> List.ofSeq

        let fetchMaterials =
            "account/materials"
            |> Api.path apiKey
            |> Api.fetch
            <!> fun response ->
                response
                |> MaterialsSchema.Parse
                |> Seq.map (fun material -> { Id = material.Id; Count = material.Count } )
                |> List.ofSeq

        [
            fetchBankItems
            fetchMaterials
        ]
        |> AsyncResult.ofParallelAsyncResults (sprintf "Fetching bank failed:\n%A") <!> List.concat

    let fetchWallet apiKey = asyncResult {
        let! response =
            "account/wallet"
            |> Api.path apiKey
            |> Api.fetch

        return
            response
            |> WalletSchema.Parse
            |> Seq.map (fun currency -> { Id = currency.Id; Amount = currency.Value } )
            |> List.ofSeq
    }

    let fetchTradingPostDelivery apiKey = asyncResult {
        let! response =
            "commerce/delivery"
            |> Api.path apiKey
            |> Api.fetch

        return
            response
            |> TradingPostDeliverySchema.Parse
            |> fun tradingPost ->
                tradingPost.Items
                |> Seq.map (fun item -> { Id = item.Id; Count = item.Count } )
            |> List.ofSeq
    }

    /// Recalculate to gold 12345 -> 1.2345 G
    let toGold value =
        (value |> int |> float) / 10000.0

    let fetchItemPrices (ids: int list) = asyncResult {
        let! response =
            ids
            |> List.map string
            |> String.concat ","
            |> sprintf "%s/commerce/listings?ids=%s" Api.BaseUrl
            |> Api.fetch

        return
            response
            |> TradingPostListingSchema.Parse
            |> Seq.map (fun item ->
                let averagePrice =
                    let averageBuys =
                        if item.Buys.Length > 0 then
                            item.Buys.[0..2]
                            |> Array.averageBy (fun i -> i.UnitPrice |> float)
                        else 0.0

                    let averageSells =
                        if item.Sells.Length > 0 then
                            item.Sells.[0..2]
                            |> Array.averageBy (fun i -> i.UnitPrice |> float)
                        else 0.0

                    [averageBuys; averageSells]
                    |> List.average
                    |> toGold

                (item.Id, averagePrice)
            )
            |> Map.ofSeq
    }

    let fetchItems (ids: int list) = asyncResult {
        let! response =
            ids
            |> List.map string
            |> String.concat ","
            |> sprintf "%s/items?ids=%s&lang=en" Api.BaseUrl
            |> Api.fetch

        return
            response
            |> ItemsSchema.Parse
            |> Seq.map (fun item ->
                item.Id, {
                    Id = item.Id
                    Name = item.Name
                }
            )
            |> Map.ofSeq
    }

    let fetchKnownRecipes apiKey = asyncResult {
        let! response =
            "account/recipes"
            |> Api.path apiKey
            |> Api.fetch

        return
            response
            |> KnownRecipeSchema.Parse
            |> List.ofSeq
    }

    let fetchRecipeUnlockId recipeId = asyncResult {
        let! response =
            recipeId
            |> sprintf "%s/items/%i" Api.BaseUrl
            |> Api.fetch

        return
            response
            |> RecipeSchema.Parse
            |> fun recipe ->
                recipe.Details.RecipeId
    }
