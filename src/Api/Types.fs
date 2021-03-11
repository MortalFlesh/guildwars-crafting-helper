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
// Currency (from wallet)
// ---------------------------

type Currency = {
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
    Currency: Currency list
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

type CurrencyItem = {
    Id: int
    Amount: int
}

[<RequireQualifiedAccess>]
module CurrencyItem =
    let getAmount { Amount = amount } = amount

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
