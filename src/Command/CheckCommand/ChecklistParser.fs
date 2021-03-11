namespace MF.GuildWars.Console.Command.CheckCommand

[<RequireQualifiedAccess>]
module ChecklistParser =
    open System.IO
    open FSharp.Data
    open MF.Api

    type private ChecklistSchema = JsonProvider<"src/Command/CheckCommand/schema/checkList.json">

    let parse checklistName: Checklist =
        let checklistData =
            checklistName
            |> File.ReadAllText
            |> ChecklistSchema.Parse

        let count =
            checklistData.Count
            |> Seq.map (fun item ->
                match item.Id with
                | Some id ->
                    let item: CountableItem = {
                        Item = {
                            Label = item.Label
                            Id = id
                        }
                        Cell = item.Cell |> SingleCell.create
                    }
                    ItemToCount.Single item
                | None ->
                    match item.Items with
                    | [||] -> failwith "There must be Items in item list"
                    | items ->
                        let itemList: CountableItemList = {
                            Label = item.Label
                            Cell = item.Cell |> RangeCell.create
                            Items =
                                items
                                |> Seq.map (fun i ->
                                    match i.Id with
                                    | Some id ->
                                        {
                                            Label = i.Label
                                            Id = id
                                        }
                                        |> ItemOrSkipped.Item
                                    | None  ->
                                        {
                                            Label = i.Label
                                        }
                                        |> ItemOrSkipped.Skipped
                                )
                                |> List.ofSeq
                        }
                        ItemToCount.Many itemList
            )
            |> List.ofSeq

        let known =
            checklistData.Known
            |> Seq.map (fun recipe ->
                {
                    Item = {
                        Label = recipe.Label
                        Id = recipe.Id
                    }
                    Cell = recipe.Cell |> SingleCell.create
                    Value = recipe.Value
                }
            )
            |> List.ofSeq

        let price =
            checklistData.Price
            |> Seq.map (fun item ->
                match item.Id with
                | Some id ->
                    let item: PriceableItem = {
                        Item = {
                            Label = item.Label
                            Id = id
                        }
                        Cell = item.Cell |> SingleCell.create
                    }
                    ItemToPrice.Single item
                | None ->
                    match item.Items with
                    | [||] -> failwith "There must be Items in item list"
                    | items ->
                        let itemList: PriceableItemList = {
                            Label = item.Label
                            Cell = item.Cell |> RangeCell.create
                            Items =
                                items
                                |> Seq.map (fun i ->
                                    match i.Id with
                                    | Some id ->
                                        {
                                            Label = i.Label
                                            Id = id
                                        }
                                        |> ItemOrSkipped.Item
                                    | None  ->
                                        {
                                            Label = i.Label
                                        }
                                        |> ItemOrSkipped.Skipped
                                )
                                |> List.ofSeq
                        }
                        ItemToPrice.Many itemList
            )
            |> List.ofSeq

        let currency =
            checklistData.Currency
            |> Seq.map (fun currency ->
                {
                    Label = currency.Label
                    Id = currency.Id
                    Cell = currency.Cell |> SingleCell.create
                }
            )
            |> List.ofSeq

        {
            Name = checklistName |> Path.GetFileName
            TabName = checklistData.TabName
            Count = count
            Known = known
            Price = price
            Currency = currency
        }
