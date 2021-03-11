namespace MF.GuildWars.Console.Command.CheckCommand

[<RequireQualifiedAccess>]
module CheckListEncoder =
    open MF.Storage
    open MF.GuildWars.Console.Command

    let encode spreadsheetId listName preparedChecklist =
        let foldEncode encode items data =
            items
            |> List.fold encode data

        let data =
            []
            |> foldEncode Encode.encodeItemWithCount preparedChecklist.Count
            |> foldEncode Encode.encodeItemWithPrice preparedChecklist.Price
            |> foldEncode Encode.encodeKnownRecipe preparedChecklist.Known
            |> foldEncode Encode.encodeCurrency preparedChecklist.Currency
            |> List.rev

        UpdateData.Float {
            SpreadsheetId = spreadsheetId
            ListName = listName
            Data = data
        }
