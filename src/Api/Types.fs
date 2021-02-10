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
    Count: ItemToCount list
    Known: Recipe list
    Price: ItemToPrice list
    Currency: Currency list
}

// ===========================
// Api
// ===========================

type ApiKey = ApiKey of string

type ItemInfo = {
    Id: int
    Name: string
}

type InventoryItem = {
    Id: int
    Count: int
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

type PricedBankItem = {
    ItemInfo: ItemInfo
    InventoryItem: InventoryItem
    Price: float
}
