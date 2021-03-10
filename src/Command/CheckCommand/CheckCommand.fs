namespace MF.GuildWars.Console.Command

[<RequireQualifiedAccess>]
module Check =
    open System.IO
    open MF.ConsoleApplication
    open MF.Config
    open MF.Api
    open MF.Storage
    open MF.ErrorHandling
    open MF.Utils
    open MF.GuildWars.Console.Command.CheckCommand
    open MF.GuildWars.Console.Command.CheckCommand.Checklist

    let private tableRows data =
        data
        |> List.map Seq.ofList
        |> Seq.ofList

    let private showLinksForTradingPost log idsToPrice =
        log.Section "Prices for checklist (link to trading post)"

        let idsToPrice =
            idsToPrice
            |> Seq.map string
            |> List.ofSeq

        idsToPrice
        |> Seq.length
        |> sprintf "Total items to price: <c:magenta>%i</c>\n"
        |> log.Message

        idsToPrice
        |> List.chunkBySize 16
        |> List.iter ((String.concat ",") >> (sprintf "https://www.gw2tp.com/custom-list?name=LIST_NAME&ids=%s\n") >> log.Message)

    let private formatCountedItem = function
        | ItemWithCount.Single i -> sprintf "%s: %i" i.Item.Item.Label i.Count
        | ItemWithCount.Many l ->
            l.Items
            |> List.map (fun i ->
                match i with
                | CountedOrSkippedItem.Skipped s -> sprintf " - %s: skipped" s.Label
                | CountedOrSkippedItem.Counted i -> sprintf " - %s: %i" i.Item.Label i.Count
            )
            |> String.concat "\n"
            |> sprintf "%s:\n%s" l.Label

    let private formatPricedItem = function
        | ItemWithPrice.Single i -> sprintf "%s: %f" i.Item.Item.Label i.Price
        | ItemWithPrice.Many l ->
            l.Items
            |> List.map (fun i ->
                match i with
                | PricedOrSkippedItem.Skipped s -> sprintf " - %s: skipped" s.Label
                | PricedOrSkippedItem.Priced i -> sprintf " - %s: %f G" i.Item.Label i.Price
            )
            |> String.concat "\n"
            |> sprintf "%s:\n%s" l.Label

    let private printLines output format list =
        list
        |> List.map format
        |> output.List

    let execute: ExecuteCommand = fun (input, output) ->
        result {
            output.Title "Check items"

            let log = {
                Section =
                    fun section ->
                        output.NewLine()
                        output.Section section
                HighlightedMessage = output.SubTitle
                Message = output.Message
            }

            let! config = Config.get (input, output)

            let itemsCache = Cache.create()
            let currencyCache = Cache.create()
            let idsCache = Cache.create()
            let pricesCache = Cache.create()

            let! checklists =
                input
                |> Input.getArgumentValueAsList "checklist"
                |> List.map (ChecklistParser.parse >> Checklist.prepareChecklist output itemsCache currencyCache idsCache pricesCache config.ApiKey)
                |> Async.Sequential
                |> Async.RunSynchronously
                |> Seq.toList
                |> Validation.ofResults
                |> Result.mapError List.concat

            checklists
            |> List.iter (fun checklist ->
                checklist.IdsToPrice
                |> showLinksForTradingPost log

                log.Section "Count items"
                checklist.Count
                |> printLines output formatCountedItem

                log.Section "Known recipes"
                checklist.Known
                |> printLines output (fun r -> sprintf "%s: %i" r.Item.Label r.Value)

                log.Section "Price items"
                checklist.Price
                |> printLines output formatPricedItem

                log.Section "Wallet items"
                checklist.Currency
                |> printLines output (fun c -> sprintf "%s: %i" c.Currency.Label c.Amount)

                // todo -- store data to sheets storage

                (* log.Section "Encode data"
                checklist
                |> encode spreadsheetId listName
                |> writeUpdateData (sprintf "%s/%s" Environment.CurrentDirectory "src/Sheets/data/update.json") *)

            )

            return "Done"
        }
        |> function
            | Ok message ->
                output.Success message
                ExitCode.Success
            | Error e ->
                e |> List.iter output.Error
                ExitCode.Error
