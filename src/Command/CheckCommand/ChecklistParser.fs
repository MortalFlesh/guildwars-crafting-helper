namespace MF.GuildWars.Console.Command.CheckCommand

[<RequireQualifiedAccess>]
module ChecklistParser =
    open System.IO
    open FSharp.Data
    open MF.Api
    open MF.ErrorHandling

    type private ChecklistSchema = JsonProvider<"src/Command/CheckCommand/schema/checkList.json">

    let parse checklistName: Result<Checklist, string> = result {
        let checklistData =
            checklistName
            |> File.ReadAllText
            |> ChecklistSchema.Parse

        let! count =
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
                    Ok (ItemToCount.Single item)
                | None ->
                    match item.Items with
                    | [||] -> Error "There must be Items in item list while counting items"
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
                        Ok (ItemToCount.Many itemList)
            )
            |> List.ofSeq
            |> Result.sequence

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

        let! price =
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
                    Ok (ItemToPrice.Single item)
                | None ->
                    match item.Items with
                    | [||] -> Error "There must be Items in item list while checking price"
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
                        Ok (ItemToPrice.Many itemList)
            )
            |> List.ofSeq
            |> Result.sequence

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

        return {
            Name = checklistName |> Path.GetFileName
            TabName = checklistData.TabName
            Count = count
            Known = known
            Price = price
            CurrencyCell = currency
        }
    }
