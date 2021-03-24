namespace MF.Api

// ============================
// Checklist
// ============================

type ExactCell = {
    Letter: string
    Number: int
}
type SingleCell = private SingleCell of string // A1
type RangeCell = private RangeCell of string // A2:A5

[<RequireQualifiedAccess>]
module SingleCell =
    let value (SingleCell single) = single
    let create (single: string) =
        if single.Contains(':') then failwithf "Single cell must not contain :"
        SingleCell single

[<RequireQualifiedAccess>]
module RangeCell =
    let value (RangeCell range) = range
    let create (range: string) =
        if range.Contains(':') |> not then failwithf "Range cells must contain :"
        RangeCell range

type Cell =
    | Single of SingleCell
    | Range of RangeCell

[<RequireQualifiedAccess>]
module Cell =
    let value = function
        | Single (SingleCell cell) -> cell
        | Range (RangeCell cell) -> cell

    let exactValue ({Letter = l; Number = n}) = sprintf "%s%i" l n

// ---------------------------
// Items
// ---------------------------

type Item = {
    Label: string
    Id: int
}

type SkippedItem = {
    Label: string
}

type ItemOrSkipped =
    | Item of Item
    | Skipped of SkippedItem

[<RequireQualifiedAccess>]
module Item =
    let getId { Id = id} = id

[<RequireQualifiedAccess>]
module ItemOrSkipped =
    let getId = function
        | Item item -> item |> Item.getId |> Some
        | Skipped _ -> None

// ---------------------------
// Count (from inventories, bank, trading post delivery, ...)
// ---------------------------

type CountableItem = {
    Item: Item
    Cell: SingleCell
}

type CountableItemList = {
    Label: string
    Items: ItemOrSkipped list
    Cell: RangeCell
}

type ItemToCount =
    | Single of CountableItem
    | Many of CountableItemList

// ---------------------------
// Known (recipe)
// ---------------------------

type Recipe = {
    Item: Item
    Cell: SingleCell
    Value: int
}

// ---------------------------
// Price (from trading post)
// ---------------------------

type PriceableItem = {
    Item: Item
    Cell: SingleCell
}

type PriceableItemList = {
    Label: string
    Items: ItemOrSkipped list
    Cell: RangeCell
}

type ItemToPrice =
    | Single of PriceableItem
    | Many of PriceableItemList

[<RequireQualifiedAccess>]
module PriceableItemList =
    let getItems ({ Items = items }: PriceableItemList) = items

// ---------------------------
// CurrencyCell (from wallet)
// ---------------------------

type CurrencyCell = {
    Label: string
    Id: int
    Cell: SingleCell
}

// ---------------------------
// Checklist
// ---------------------------

type Checklist = {
    Name: string
    TabName: string
    Count: ItemToCount list
    Known: Recipe list
    Price: ItemToPrice list
    CurrencyCell: CurrencyCell list
}

// ===========================
// Api
// ===========================

type ApiKey = ApiKey of string

type CharacterName = CharacterName of string

type Rarity =
    | Lower
    | Fine
    | Masterwork
    | Rare
    | Exotic
    | Ascended
    | Legendary

type Binding =
    | AccountBound
    | SoulBound of CharacterName
    | Unbound

type ItemInfo = {
    Id: int
    Name: string
    Rarity: Rarity
}

type InventoryItem = {
    Id: int
    Count: int
    Binding: Binding
}

type Inventory = InventoryItem list

type CurrencyName =
    | Gold
    | Karma
    | Laurel
    | Gem
    // Dungeons
    | AscalonianTear | ManifestoOfTheMoletariat | DeadlyBloom | SymbolOfKoda | FlameLegionCharrCarvings
    | FractalRelic | PristineFractalRelic
    // WvW
    | BadgeOfHonor | WvWSkirmishClaimTicket
    | GuildComendation
    | TransmutationCharge
    // HoT
    | AirshipPart | LeyLineCrystal | LumpOfAurilium | ExaltedKey | Machette | PactCrowbar | VialOfChak
    | SpiritShard
    // LWS2
    | Geode | BanditCrest
    // Raid
    | MagnetiteShard
    | ProvisionerToken
    // PVP
    | PvPLeagueTicket | AscendedShardOfGlory
    // LWS3
    | UnboundMagic
    // PoF
    | TradeContract | ElegyMosaic | TradersKey
    | TestimonyOfHeroic
    | ZephyriteLockpick
    // LWS4
    | VolatileMagic
    | RacingMedailon
    | MistbornKey
    | FestivalToken
    // IceBrood Saga
    | CacheKey | RedProphetShard | GreenProphetShard | BlueProphetCrystal | BlueProphetShard | WarSupplies | TyrianDefenseSeal

    | NotDefined of int

[<RequireQualifiedAccess>]
module CurrencyName =
    let parse = function
        | 1 -> Gold
        | 2 -> Karma
        | 3 -> Laurel
        | 4 -> Gem
        | 5 -> AscalonianTear
        | 7 -> FractalRelic
        | 10 -> ManifestoOfTheMoletariat
        | 11 -> DeadlyBloom
        | 12 -> SymbolOfKoda
        | 13 -> FlameLegionCharrCarvings
        | 15 -> BadgeOfHonor
        | 16 -> GuildComendation
        | 18 -> TransmutationCharge
        | 19 -> AirshipPart
        | 20 -> LeyLineCrystal
        | 22 -> LumpOfAurilium
        | 23 -> SpiritShard
        | 24 -> PristineFractalRelic
        | 25 -> Geode
        | 26 -> WvWSkirmishClaimTicket
        | 27 -> BanditCrest
        | 28 -> MagnetiteShard
        | 29 -> ProvisionerToken
        | 30 -> PvPLeagueTicket
        | 32 -> UnboundMagic
        | 33 -> AscendedShardOfGlory
        | 34 -> TradeContract
        | 35 -> ElegyMosaic
        | 36 -> TestimonyOfHeroic
        | 37 -> ExaltedKey
        | 38 -> Machette
        | 41 -> PactCrowbar
        | 42 -> VialOfChak
        | 43 -> ZephyriteLockpick
        | 44 -> TradersKey
        | 45 -> VolatileMagic
        | 47 -> RacingMedailon
        | 49 -> MistbornKey
        | 50 -> FestivalToken
        | 51 -> CacheKey
        | 52 -> RedProphetShard
        | 53 -> GreenProphetShard
        | 54 -> BlueProphetCrystal
        | 57 -> BlueProphetShard
        | 58 -> WarSupplies
        | 60 -> TyrianDefenseSeal

        | notDefined -> NotDefined notDefined

type Currency = {
    Id: int
    Amount: float
    Name: CurrencyName
}

[<RequireQualifiedAccess>]
module Currency =
    let name ({ Name = name }: Currency) = name
    let amount { Amount = amount } = amount

type Wallet = Currency list

// ===========================
// Domain
// ===========================

[<RequireQualifiedAccess>]
module CharacterName =
    let value (CharacterName name) = name

type ItemWithInfo = {
    ItemInfo: ItemInfo
    InventoryItem: InventoryItem
}

type ItemWithInfoAndPrice = {
    ItemInfo: ItemInfo
    InventoryItem: InventoryItem
    Price: float
}

type RawEquipmentInfo = {
    Id: int
    Slot: string
    Upgrades: int list
    Infusions: int list
    Binding: Binding
    // todo - stats, dyes, skin,
}

type FullItem = {
    Id: int
    Name: string
    Count: int
    Price: float option
    TotalPrice: float option
    Rarity: Rarity
    Upgrades: FullItem list
    Infusions: FullItem list
    Binding: Binding
    // todo - stats, dyes, skin,
}

type Character = {
    Name: CharacterName
    Inventory: Bag list
    Equipment: Equipment
}

and Bag = {
    Info: FullItem
    Size: int
    Inventory: FullItem list
}

and Equipment = {
    Head: FullItem option
    Shoulders: FullItem option
    Chest: FullItem option
    Hands: FullItem option
    Legs: FullItem option
    Feet: FullItem option

    Back: FullItem option
    Trinket1: FullItem option
    Trinket2: FullItem option
    Amulet: FullItem option
    Ring1: FullItem option
    Ring2: FullItem option

    WeaponA1: FullItem option
    WeaponA2: FullItem option
    WeaponB1: FullItem option
    WeaponB2: FullItem option
}

[<RequireQualifiedAccess>]
module Rarity =
    let parse = function
        | "Legendary" -> Legendary
        | "Ascended" -> Ascended
        | "Exotic" -> Exotic
        | "Rare" -> Rare
        | "Masterwork" -> Masterwork
        | "Fine" -> Fine
        | _ -> Lower

    let value = function
        | Legendary -> "(leg)"
        | Ascended -> "(asc)"
        | Exotic -> "(exo)"
        | Rare -> "(rar)"
        | Masterwork -> "(mas)"
        | Fine -> "(fin)"
        | _ -> ""

[<RequireQualifiedAccess>]
module Binding =
    let parse = function
        | (Some "Character", Some bindTo) -> SoulBound (CharacterName bindTo)
        | (Some "Account", _) -> AccountBound
        | _ -> Unbound

[<RequireQualifiedAccess>]
module Bag =
    let inventory ({ Inventory = inventory }: Bag) = inventory

[<RequireQualifiedAccess>]
module Equipment =
    let empty = {
        Head = None
        Shoulders = None
        Chest = None
        Hands = None
        Legs = None
        Feet = None

        Back = None
        Trinket1 = None
        Trinket2 = None
        Amulet = None
        Ring1 = None
        Ring2 = None

        WeaponA1 = None
        WeaponA2 = None
        WeaponB1 = None
        WeaponB2 = None
    }

    let private items (equipment: Equipment): FullItem list =
        [
            yield! equipment.Head |> Option.toList
            yield! equipment.Shoulders |> Option.toList
            yield! equipment.Chest |> Option.toList
            yield! equipment.Hands |> Option.toList
            yield! equipment.Legs |> Option.toList
            yield! equipment.Feet |> Option.toList

            yield! equipment.Back |> Option.toList
            yield! equipment.Trinket1 |> Option.toList
            yield! equipment.Trinket2 |> Option.toList
            yield! equipment.Amulet |> Option.toList
            yield! equipment.Ring1 |> Option.toList
            yield! equipment.Ring2 |> Option.toList

            yield! equipment.WeaponA1 |> Option.toList
            yield! equipment.WeaponA2 |> Option.toList
            yield! equipment.WeaponB1 |> Option.toList
            yield! equipment.WeaponB2 |> Option.toList
        ]

    let count = items >> List.length
