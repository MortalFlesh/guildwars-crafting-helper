namespace GuildWarsHelper

open ApiProvider

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
