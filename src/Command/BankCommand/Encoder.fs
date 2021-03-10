namespace MF.GuildWars.Console.Command.BankCommand

[<RequireQualifiedAccess>]
module BankEncoder =
    open MF.Api
    open MF.Storage
    open MF.GuildWars.Console.Command

    let encodeItems spreadsheetId listName startingCell pricedItems =
        let data = []
        let totalItems = pricedItems |> List.length

        // A2 ... A100 -> i.names
        let cell = startingCell
        let nameRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let nameData =
            pricedItems
            |> List.fold Encode.encodeItemNames []
        let data = (nameRange, nameData) :: data

        // B2 ... B100 -> i.count
        let cell = { startingCell with Letter = "B" }
        let countRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let countData =
            pricedItems
            |> List.fold Encode.encodeItemCount []
        let data = (countRange, countData) :: data

        // C2 ... C100 -> i.pricePerPiece
        let cell = { startingCell with Letter = "C" }
        let pricePerPieceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let pricePerPieceData =
            pricedItems
            |> List.fold Encode.encodePricePerPiece []
        let data = (pricePerPieceRange, pricePerPieceData) :: data

        // D2 ... D100 -> i.price * count
        let cell = { cell with Letter = "D" }
        let totalPriceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let totalPriceData =
            pricedItems
            |> List.fold Encode.encodeTotalPrice []
        let data = (totalPriceRange, totalPriceData) :: data

        UpdateData.String {
            SpreadsheetId = spreadsheetId
            ListName = listName
            Data = data |> List.rev
        }
