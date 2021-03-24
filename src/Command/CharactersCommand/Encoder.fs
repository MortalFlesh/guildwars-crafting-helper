namespace MF.GuildWars.Console.Command.CharactersCommand

[<RequireQualifiedAccess>]
module CharactersEncoder =
    open MF.Api
    open MF.Storage
    open MF.GuildWars.Console.Command

    type Cells = {
        Name: ExactCell
        Inventory: ExactCell
        Equipment: ExactCell
    }

    let private encodeBags startingCell bags =
        let data = []
        let totalItems =
            let totalBags = bags |> List.length
            let totalItems = bags |> List.sumBy (Bag.inventory >> List.length)
            totalBags + totalItems

        let moveLetter = GoogleSheets.letterMoveBy 1

        // A2 ... A100 -> i.names
        let cell = startingCell
        let nameRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let nameData =
            bags
            |> List.collect (fun bag ->
                [
                    yield! bag.Info |> Encode.FullItem.encodeName []
                    yield! bag.Inventory |> List.fold Encode.FullItem.encodeName []
                ]
            )
        let data = (nameRange, nameData) :: data

        // B2 ... B100 -> i.count
        let cell = { startingCell with Letter = startingCell.Letter |> moveLetter 1 }
        let countRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let countData =
            bags
            |> List.collect (fun bag ->
                [
                    [ "" ]
                    yield! bag.Inventory |> List.fold Encode.FullItem.encodeCount []
                ]
            )
        let data = (countRange, countData) :: data

        // C2 ... C100 -> i.pricePerPiece
        let cell = { startingCell with Letter = startingCell.Letter |> moveLetter 2 }
        let pricePerPieceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let pricePerPieceData =
            bags
            |> List.collect (fun bag ->
                [
                    [ "---" ]
                    yield! bag.Inventory |> List.fold Encode.FullItem.encodePrice []
                ]
            )
        let data = (pricePerPieceRange, pricePerPieceData) :: data

        // D2 ... D100 -> i.price * count
        let cell = { cell with Letter = startingCell.Letter |> moveLetter 3 }
        let totalPriceRange = sprintf "%s:%s" (cell |> Cell.exactValue) ({cell with Number = cell.Number + (totalItems)} |> Cell.exactValue)
        let totalPriceData =
            bags
            |> List.collect (fun bag ->
                [
                    [ "---" ]
                    yield! bag.Inventory |> List.fold Encode.FullItem.encodeTotalPrice []
                ]
            )
        let data = (totalPriceRange, totalPriceData) :: data

        data |> List.rev

    let private encodeEquipment cell equipment =
        let encodeItem (item: FullItem) =
            let concat (items: string list) =
                items
                |> List.map (fun s -> s.Trim ' ')
                |> String.concat ", "

            [[
                sprintf "%s %s" item.Name (item.Rarity |> Rarity.value)

                item.Upgrades
                |> List.map (fun upgrade -> upgrade.Name) |> concat

                item.Infusions
                |> List.groupBy (fun infusion -> infusion.Name)
                |> List.map (fun (name, infusions) ->
                    let count = infusions |> List.length
                    sprintf "%s %s" name (if count > 1 then sprintf "[%d]" count else "")
                )
                |> concat
            ]]

        let range cell =
            let moveLetter = GoogleSheets.letterMoveBy 1

            sprintf "%s:%s"
                (cell |> Cell.exactValue)
                ({cell with Letter = cell.Letter |> moveLetter 2 } |> Cell.exactValue)

        let encode cell item =
            range cell,
            item |> Option.map encodeItem |> Option.defaultValue []

        [
            equipment.Head |> encode { Letter = cell.Letter; Number = cell.Number + 0 }
            equipment.Shoulders |> encode { Letter = cell.Letter; Number = cell.Number + 1 }
            equipment.Chest |> encode { Letter = cell.Letter; Number = cell.Number + 2 }
            equipment.Hands |> encode { Letter = cell.Letter; Number = cell.Number + 3 }
            equipment.Legs |> encode { Letter = cell.Letter; Number = cell.Number + 4 }
            equipment.Feet |> encode { Letter = cell.Letter; Number = cell.Number + 5 }
            equipment.Back |> encode { Letter = cell.Letter; Number = cell.Number + 6 }
            equipment.Trinket1 |> encode { Letter = cell.Letter; Number = cell.Number + 7 }
            equipment.Trinket2 |> encode { Letter = cell.Letter; Number = cell.Number + 8 }
            equipment.Amulet |> encode { Letter = cell.Letter; Number = cell.Number + 9 }
            equipment.Ring1 |> encode { Letter = cell.Letter; Number = cell.Number + 10 }
            equipment.Ring2 |> encode { Letter = cell.Letter; Number = cell.Number + 11 }
            equipment.WeaponA1 |> encode { Letter = cell.Letter; Number = cell.Number + 12 }
            equipment.WeaponA2 |> encode { Letter = cell.Letter; Number = cell.Number + 13 }
            equipment.WeaponB1 |> encode { Letter = cell.Letter; Number = cell.Number + 14 }
            equipment.WeaponB2 |> encode { Letter = cell.Letter; Number = cell.Number + 15 }
        ]

    let encode spreadsheetId listName (cells: Cells) (character: Character) =
        let characterNameData =
            cells.Name |> Cell.exactValue,
            [[ (character.Name |> CharacterName.value) ]]

        let pricedInventoryData =
            character.Inventory
            |> encodeBags cells.Inventory

        let equipmentData =
            character.Equipment
            |> encodeEquipment cells.Equipment

        UpdateData.String {
            SpreadsheetId = spreadsheetId
            ListName = listName
            Data = characterNameData :: pricedInventoryData @ equipmentData
        }
