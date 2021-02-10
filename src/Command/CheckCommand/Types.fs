namespace MF.GuildWars.Console.Command.CheckCommand

open MF.Api

type Log = {
    Section: string -> unit
    HighlightedMessage: string -> unit
    Message: string -> unit
}

// ===========================
// Count
// ===========================

type CountedItem = {
    Item: CountableItem
    Count: int
}

type CountedListItem = {
    Item: Item
    Count: int
}

type CountedOrSkippedItem =
    | Counted of CountedListItem
    | Skipped of SkippedItem

type CountedItemList = {
    Label: string
    Items: CountedOrSkippedItem list
    Cell: RangeCell
}

type ItemWithCount =
    | Single of CountedItem
    | Many of CountedItemList

// ===========================
// Price
// ===========================

type PricedItem = {
    Item: PriceableItem
    Price: float
}

type PricedListItem = {
    Item: Item
    Price: float
}

type PricedOrSkippedItem =
    | Priced of PricedListItem
    | Skipped of SkippedItem

type PricedItemList = {
    Label: string
    Items: PricedOrSkippedItem list
    Cell: RangeCell
}

type ItemWithPrice =
    | Single of PricedItem
    | Many of PricedItemList

// ===========================
// Currency
// ===========================

type CurrencyWithAmount = {
    Currency: Currency
    Amount: int
}

// ===========================
// Prepared checklist
// ===========================

type PreparedChecklist = {
    Name: string
    Count: ItemWithCount list
    Known: Recipe list
    Price: ItemWithPrice list
    IdsToPrice: int list
    Currency: CurrencyWithAmount list // todo
}
