namespace ApiProvider

// ============================
// Checklist
// ============================

type SingleCell = private SingleCell of string // A1
type RangeCell = private RangeCell of string // A2:A5

module SingleCell =
    let value (SingleCell single) = single
    let create (single: string) =
        if single.Contains(':') then failwithf "Single cell must not contain :"
        SingleCell single

module RangeCell =
    let value (RangeCell range) = range
    let create (range: string) =
        if range.Contains(':') |> not then failwithf "Range cells must contain :"
        RangeCell range

type Cell =
    | Single of SingleCell
    | Range of RangeCell

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

[<RequireQualifiedAccessAttribute>]
module Item =
    let getId { Id = id} = id

[<RequireQualifiedAccessAttribute>]
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

[<RequireQualifiedAccessAttribute>]
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
    Count: ItemToCount list
    Known: Recipe list
    Price: ItemToPrice list
    Currency: Currency list
}

// ===========================
// Api
// ===========================

type InventoryItem = {
    Id: int
    Count: int
}

type Inventory = InventoryItem list

type CurrencyItem = {
    Id: int
    Amount: int
}

module CurrencyItem =
    let getAmount { Amount = amount } = amount
