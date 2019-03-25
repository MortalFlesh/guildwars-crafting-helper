namespace Sheets

module Api =
    open System.IO
    open Newtonsoft.Json

    let private serialize obj =
        JsonConvert.SerializeObject obj

    let private serializePretty obj =
        JsonConvert.SerializeObject(obj, Formatting.Indented)

    let writeUpdateData path = function
        | UpdateData.Float data ->
            data
            |> serialize
            |> fun data -> (path, data)
            |> File.WriteAllText

        | UpdateData.String data ->
            data
            |> serialize
            |> fun data -> (path, data)
            |> File.WriteAllText
