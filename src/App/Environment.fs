module Environment

open System
open System.IO
open System.Text.RegularExpressions

// inspired by http://www.codesuji.com/2018/02/28/F-and-DotEnv/
let private loadDotEnv error envFile =
    let envFile = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", envFile)

    if IO.File.Exists(envFile)
    then
        IO.File.ReadAllLines(envFile)
        |> Seq.iter (fun line ->
            let lineWithoutComments = Regex.Replace(line, "#.*", "")
            let parts =
                lineWithoutComments.Split('=')
                |> Array.map (fun x -> x.Trim())

            match parts with
            | [| key; value |] when String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)) ->
                Environment.SetEnvironmentVariable(key, value.Trim([|'"'; '''|]))
            | _ -> ()
        )
    else
        error (sprintf "File %s does not exists." envFile)

let getEnvs error envFile =
    loadDotEnv error envFile

    // https://stackoverflow.com/questions/12014179/f-get-environment-variables-as-a-generic-collection
    System.Environment.GetEnvironmentVariables()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> Map

let tryGetEnv (envs: Map<string, string>) key =
    envs |> Map.tryFind key

let getEnv (envs: Map<string, string>) key =
    match key |> tryGetEnv envs with
    | Some value -> value
    | _ -> failwithf "[Error]: ENV variable \"%s\" was not defined." key
