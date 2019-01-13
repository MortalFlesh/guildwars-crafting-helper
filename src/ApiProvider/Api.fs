namespace ApiProvider

module Api =
    open FSharp.Data
    open Configuration

    type private CharactersSchema = JsonProvider<const(BaseUrl + "/characters?access_token=" + ApiKey)>
    type private InventorySchema = JsonProvider<"schema/inventory.json">
    type private BankSchema = JsonProvider<const(BaseUrl + "/account/bank?access_token=" + ApiKey)>
    type private Materials = JsonProvider<const(BaseUrl + "/account/materials?access_token=" + ApiKey)>
    type private WalletSchema = JsonProvider<const(BaseUrl + "/account/wallet?access_token=" + ApiKey)>
    type private TradingPostDeliverySchema = JsonProvider<"schema/tradingPostDelivery.json", SampleIsList=true>

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
