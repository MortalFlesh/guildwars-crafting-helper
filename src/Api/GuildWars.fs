namespace MF.Api

[<RequireQualifiedAccess>]
module GuildWars =
    open System
    open FSharp.Data
    open MF.Utils
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators

    type private CharactersSchema = JsonProvider<"schema/characters.json">
    type private CharacterSchema = JsonProvider<"schema/character.json">
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

    let fetchInventory apiKey (CharacterName character): AsyncResult<Inventory, _> = asyncResult {
        let! response =
            sprintf "characters/%s" character
            |> Api.path apiKey
            |> Api.fetch

        let data =
            response
            |> CharacterSchema.Parse

        return
            data.Bags
            |> Seq.collect (fun bag ->
                bag.Inventory
                |> Seq.map (fun item -> {
                    Id = item.Id
                    Count = item.Count
                    Binding = (item.Binding, item.BoundTo) |> Binding.parse
                })
            )
            |> List.ofSeq
    }

    let fetchCharacters apiKey: AsyncResult<CharacterName list, string list> = asyncResult {
        let! response =
            "characters"
            |> Api.path apiKey
            |> Api.fetch <@> List.singleton

        let characters =
            response
            |> CharactersSchema.Parse

        return
            characters
            |> Seq.map CharacterName
            |> Seq.sort
            |> Seq.toList
    }

    let fetchCharactersInventories apiKey: AsyncResult<Inventory, string list> = asyncResult {
        let! characters = apiKey |> fetchCharacters

        return!
            characters
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
                |> Seq.map (fun item -> {
                    Id = item.Id
                    Count = item.Count
                    Binding = (item.Binding, None) |> Binding.parse
                })
                |> List.ofSeq

        let fetchMaterials =
            "account/materials"
            |> Api.path apiKey
            |> Api.fetch
            <!> fun response ->
                response
                |> MaterialsSchema.Parse
                |> Seq.map (fun material -> { Id = material.Id; Count = material.Count; Binding = Unbound } )
                |> List.ofSeq

        [
            fetchBankItems
            fetchMaterials
        ]
        |> AsyncResult.ofParallelAsyncResults (sprintf "Fetching bank failed:\n%A") <!> List.concat

    /// Recalculate to gold 12345 -> 1.2345 G
    let toGold value =
        (value |> int |> float) / 10000.0

    let fetchWallet apiKey: AsyncResult<Wallet, string> = asyncResult {
        let! response =
            "account/wallet"
            |> Api.path apiKey
            |> Api.fetch

        return
            response
            |> WalletSchema.Parse
            |> Seq.map (fun currency ->
                let name = CurrencyName.parse currency.Id

                {
                    Id = currency.Id
                    Name = name
                    Amount =
                        match name with
                        | Gold -> currency.Value |> float |> toGold
                        | _ -> currency.Value |> float
                }
            )
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
                |> Seq.map (fun item -> { Id = item.Id; Count = item.Count; Binding = Unbound } )
            |> List.ofSeq
    }

    let fetchItemPrices (ids: int list) = asyncResult {
        let! response =
            ids
            |> List.map string
            |> String.concat ","
            |> sprintf "%s/commerce/listings?ids=%s" Api.BaseUrl
            |> Api.fetch
            >>- (function
                | allInvalid when allInvalid.Contains "all ids provided are invalid" -> AsyncResult.ofSuccess "[]"
                | e -> AsyncResult.ofError e
            )

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
                let info: ItemInfo = {
                    Id = item.Id
                    Name = item.Name
                    Rarity = item.Rarity |> Rarity.parse
                }

                item.Id, info
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

    [<RequireQualifiedAccess>]
    type BulkMode =
        | Single
        | Bulk

    let fetchPricedItems (output: MF.ConsoleApplication.Output) bulkMode (inventory: Inventory) =
        let teeNote f a =
            match bulkMode with
            | BulkMode.Single -> f a
            | BulkMode.Bulk -> ()
            a

        let chunks =
            inventory
            |> List.filter (fun { Count = count } -> count > 0)
            |> teeNote (List.length >> sprintf "<c:dark-yellow>Note:</c> There are <c:magenta>%A</c> items after filter out uncountable items" >> output.Message >> output.NewLine)
            |> List.chunkBySize 100

        let progress =
            match bulkMode with
            | BulkMode.Bulk -> None
            | BulkMode.Single ->
                chunks
                |> List.length
                |> output.ProgressStart (chunks |> List.length |> sprintf "Fetching details for %A chunks ...")
                |> Some

        chunks
        |> List.map (fun items -> asyncResult {
            let itemIds =
                items
                |> List.map (fun { Id = id } -> id)

            let! prices =
                itemIds
                |> fetchItemPrices

            let! itemsInfo =
                itemIds
                |> fetchItems

            return
                items
                |> List.map (fun item ->
                    {
                        ItemInfo = itemsInfo.[item.Id]
                        InventoryItem = item
                        Price =
                            prices
                            |> Map.tryFind item.Id
                            |> function
                                | Some float -> float
                                | _ -> 0.0
                    }
                )
                |> List.filter (fun i -> i.Price > 0.0)
                |> tee (fun _ -> progress |> Option.iter output.ProgressAdvance)
        })
        |> AsyncResult.ofSequentialAsyncResults (sprintf "Error while fetching bank item details: %A")
        <!> List.concat
        >>* (fun items ->
            progress |> Option.iter output.ProgressFinish

            if bulkMode = BulkMode.Single then
                items |> List.length |> sprintf "Fetched %A items" |> output.Success
        )

    let fetchUnpricedItems (inventory: Inventory): AsyncResult<ItemWithInfo list, _> =
        let chunks =
            inventory
            |> List.filter (fun { Count = count } -> count > 0)
            |> List.chunkBySize 100

        chunks
        |> List.map (fun items -> asyncResult {
            let itemIds =
                items
                |> List.map (fun { Id = id } -> id)

            let! itemsInfo =
                itemIds
                |> fetchItems

            return
                items
                |> List.map (fun item ->
                    {
                        ItemInfo = itemsInfo.[item.Id]
                        InventoryItem = item
                    }
                )
        })
        |> AsyncResult.ofParallelAsyncResults (sprintf "Error while fetching bank item details: %A")
        <!> List.concat

    let rec fetchFullItems (output: MF.ConsoleApplication.Output) bulkMode (equipmentInfo: Map<int, RawEquipmentInfo>) (inventory: Inventory) = asyncResult {
        let teeNote f a =
            if output.IsVerbose() then
                match bulkMode with
                | BulkMode.Single -> f a
                | BulkMode.Bulk -> ()
            a

        let chunks =
            inventory
            |> teeNote (List.length >> sprintf "<c:dark-yellow>Note:</c> There are <c:magenta>%A</c> items to chunk" >> output.Message >> output.NewLine)
            |> List.chunkBySize 100

        let idsToInfo ids =
            ids
            |> List.distinct
            |> List.map (fun upgrade -> { Id = upgrade; Count = 1; Binding = Unbound })

        let equipmentList =
            equipmentInfo
            |> Map.toList
            |> List.map snd

        let! upgradesInfo =
            equipmentList
            |> List.collect (fun { Upgrades = upgrades } -> upgrades)
            |> teeNote (List.length >> sprintf "<c:dark-yellow>Note:</c> Fetch <c:magenta>%A</c> upgrades ..." >> output.Message)
            |> idsToInfo
            |> function
                | [] -> AsyncResult.ofSuccess Map.empty
                | upgrades -> upgrades |> fetchFullItems output BulkMode.Bulk Map.empty

        let! infusionsInfo =
            equipmentList
            |> List.collect (fun { Infusions = infusions } -> infusions)
            |> teeNote (List.length >> sprintf "<c:dark-yellow>Note:</c> Fetch <c:magenta>%A</c> infusions ..." >> output.Message)
            |> idsToInfo
            |> function
                | [] -> AsyncResult.ofSuccess Map.empty
                | infusions -> infusions |> fetchFullItems output BulkMode.Bulk Map.empty

        return!
            chunks
            |> teeNote (List.length >> sprintf "<c:dark-yellow>Note:</c> Fetch <c:magenta>%A</c> chunks ..." >> output.Message)
            |> List.map (fun items -> asyncResult {
                let! prices =
                    items
                    |> List.filter (fun { Count = count } -> count > 0)
                    |> List.map (fun { Id = id } -> id)
                    |> fetchItemPrices

                let! itemsInfo =
                    items
                    |> List.map (fun { Id = id } -> id)
                    |> fetchItems

                return
                    items
                    |> List.map (fun item ->
                        let price = prices |> Map.tryFind item.Id
                        let info = itemsInfo.[item.Id]
                        let binding, upgrades, infusions =
                            match equipmentInfo.TryFind item.Id with
                            | Some info ->
                                info.Binding,
                                info.Upgrades |> List.choose (fun id -> upgradesInfo |> Map.tryFind id),
                                info.Infusions |> List.choose (fun id -> infusionsInfo |> Map.tryFind id)
                            | _ -> Unbound, [], []

                        {
                            Id = item.Id
                            Name = info.Name
                            Count = item.Count
                            Price = price
                            TotalPrice = price |> Option.map ((*) (float item.Count))
                            Rarity = info.Rarity
                            Upgrades = upgrades
                            Infusions = infusions
                            Binding = binding
                        }
                    )
            })
            |> AsyncResult.ofSequentialAsyncResults (sprintf "Error while fetching bank item details: %A")
            <!> (List.concat >> List.map (fun item -> item.Id, item) >> Map.ofList) // todo - this could be a problem for multiple items of the same id in the list
            >>* (fun items ->
                if bulkMode = BulkMode.Single then
                    items |> Map.count |> sprintf "Fetched %A items" |> output.Success
            )
    }

    let fetchCharacter (output: MF.ConsoleApplication.Output) apiKey bulkMode (CharacterName character): AsyncResult<_, _> = asyncResult {
        match bulkMode with
        | BulkMode.Single -> character |> sprintf "Fetching %s" |> output.Section
        | _ -> ()

        let subTitle message =
            match bulkMode with
            | BulkMode.Single -> output.SubTitle <| sprintf " -= %s =-" message
            | _ -> ()

        let! response =
            sprintf "characters/%s" character
            |> Api.path apiKey
            |> Api.fetch <@> List.singleton

        let data = response |> CharacterSchema.Parse
        let bagsData = data.Bags |> Seq.toList
        let equipmentData = data.Equipment |> Seq.toList

        subTitle "Bags items"
        let! bagsItemsInfo =
            (
                bagsData
                |> List.map (fun bag ->
                    { Id = bag.Id; Count = 1; Binding = Unbound
                })
            )
            @ (
                bagsData
                |> List.collect (fun bag ->
                    bag.Inventory
                    |> Seq.toList
                    |> List.map (fun item ->
                        { Id = item.Id; Count = item.Count; Binding = (item.Binding, item.BoundTo) |> Binding.parse }
                    )
                )
            )
            |> fetchFullItems output bulkMode Map.empty

        let bags =
            bagsData
            |> List.map (fun bag ->
                {
                    Info = bagsItemsInfo.[bag.Id]
                    Size = bag.Size
                    Inventory =
                        bag.Inventory
                        |> Seq.map (fun item -> bagsItemsInfo.[item.Id])
                        |> Seq.toList
                }
            )

        let equipmentInfo =
            equipmentData
            |> List.map (fun equip ->
                equip.Id, {
                    Id = equip.Id
                    Slot = equip.Slot
                    Upgrades = equip.Upgrades |> Seq.toList
                    Infusions = equip.Infusions |> Seq.toList
                    Binding = (Some equip.Binding, equip.BoundTo) |> Binding.parse
                }
            )
            |> Map.ofList

        subTitle "Equipment items"
        let! equpmentItemsInfo =
            equipmentData
            |> List.map (fun equip ->
                { Id = equip.Id; Count = 1; Binding = (Some equip.Binding, equip.BoundTo) |> Binding.parse }
            )
            |> fetchFullItems output bulkMode equipmentInfo

        let equipment =
            equipmentData
            |> List.fold (fun equipment item ->
                let info = equpmentItemsInfo.[item.Id]

                match item.Slot with
                | "Helm" -> { equipment with Head = Some info }
                | "Shoulders" -> { equipment with Shoulders = Some info }
                | "Coat" -> { equipment with Chest = Some info }
                | "Gloves" -> { equipment with Hands = Some info }
                | "Leggings" -> { equipment with Legs = Some info }
                | "Boots" -> { equipment with Feet = Some info }

                | "Backpack" -> { equipment with Back = Some info }
                | "Accessory1" -> { equipment with Trinket1 = Some info }
                | "Accessory2" -> { equipment with Trinket2 = Some info }
                | "Amulet" -> { equipment with Amulet = Some info }
                | "Ring1" -> { equipment with Ring1 = Some info }
                | "Ring2" -> { equipment with Ring2 = Some info }

                | "WeaponA1" -> { equipment with WeaponA1 = Some info }
                | "WeaponA2" -> { equipment with WeaponA2 = Some info }
                | "WeaponB1" -> { equipment with WeaponB1 = Some info }
                | "WeaponB2" -> { equipment with WeaponB2 = Some info }

                | "Sickle" | "Axe" | "Pick"
                | _ -> equipment

            ) Equipment.empty

        return {
            Name = CharacterName character
            Inventory = bags
            Equipment = equipment
        }
    }
