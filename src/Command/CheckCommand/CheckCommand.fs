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
    open MF.GuildWars.Console.Command
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

    let private logLines (output: Output) format list =
        list
            |> List.length
            |> sprintf " -> <c:green>Done</c> with <c:magenta>%A</c> items"
            |> output.Message

        if output.IsVerbose() then
            list
            |> List.map format
            |> output.List

    let execute: ExecuteCommand = fun (input, output) ->
        result {
            output.Title "Check items"

            let log checklist = {
                Section =
                    fun section ->
                        output.NewLine()
                        output.Section <| sprintf "[%s] %s" checklist section
                HighlightedMessage = sprintf "[%s] %s" checklist >> output.SubTitle
                Message = sprintf "[%s] %s" checklist >> output.Message
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

            do!
                checklists
                |> List.map (fun checklist -> asyncResult {
                    let log =
                        let (TabName name) = checklist.TabName
                        log name

                    checklist.IdsToPrice
                    |> showLinksForTradingPost log

                    log.Section "Count items"
                    checklist.Count
                    |> logLines output formatCountedItem

                    log.Section "Known recipes"
                    checklist.Known
                    |> logLines output (fun r -> sprintf "%s: %i" r.Item.Label r.Value)

                    log.Section "Price items"
                    checklist.Price
                    |> logLines output formatPricedItem

                    log.Section "Wallet items"
                    checklist.Currency
                    |> logLines output (fun c -> sprintf "%s: %i" c.Currency.Label c.Amount)

                    log.Section "Encode data"
                    let encodedCheckList =
                        checklist
                        |> CheckListEncoder.encode config.GoogleSheets.SpreadsheetId checklist.TabName

                    log.Section "Update google sheets"
                    let logSheets message =
                        if output.IsVerbose() then
                            output.Message <| sprintf "[Sheets] %s" message

                    return!
                        encodedCheckList
                        |> GoogleSheets.updateSheets logSheets config.GoogleSheets
                })
                |> AsyncResult.ofSequentialAsyncResults (sprintf "%A")
                |> AsyncResult.map ignore
                |> Async.RunSynchronously

            return "Done"
        }
        |> function
            | Ok message ->
                output.Success message
                ExitCode.Success
            | Error e ->
                e |> List.iter output.Error
                ExitCode.Error
