// Learn more about F# at http://fsharp.org

open System
//open MF.ConsoleStyle
open ApiProvider.Characters

[<EntryPoint>]
let main argv =
    Console.title "Hello from Guild Wars helper"

    let tableRows data =
        data
        |> List.map Seq.ofList
        |> Seq.ofList

    fetchCharacters()
    |> tableRows
    |> Console.table ["Name"]

    0
