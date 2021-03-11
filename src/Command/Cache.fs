namespace MF.GuildWars.Console.Command

open System.Collections.Concurrent

type Cache<'Data> = Cache of ConcurrentDictionary<string, 'Data>

[<RequireQualifiedAccess>]
module Cache =
    open MF.ErrorHandling
    open MF.ErrorHandling.AsyncResult.Operators

    let create<'Data> () = ConcurrentDictionary<string, 'Data>() |> Cache

    let fetchWithCache<'Data, 'Error>
        (output: MF.ConsoleApplication.Output)
        (Cache cache: Cache<'Data>)
        key
        (fetch: AsyncResult<'Data, 'Error>)
        : AsyncResult<'Data, 'Error> =

        if output.IsDebug() then
            output.Message <| sprintf "Available.keys: %A" cache.Keys

        let log message =
            if output.IsVerbose() then
                output.Message message

        match cache.TryGetValue key with
        | true, data ->
            data
            |> AsyncResult.ofSuccess
            >>* (fun _ -> sprintf " - %s from cache" key |> log)
        | _ ->
            fetch
            >>* (fun data ->
                sprintf " - %s fetched" key |> log

                cache.[key] <- data
                sprintf " - %s data cached" key |> log
            )
