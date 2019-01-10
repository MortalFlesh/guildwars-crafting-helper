namespace ApiProvider

module Characters =
    open FSharp.Data
    open Configuration

    type private CharactersSchema = JsonProvider<const(BaseUrl + "/characters?access_token=" + ApiKey)>

    let fetchCharacters () =
        CharactersSchema.GetSamples()
        |> Array.map (fun i ->
            [i]
        )
        |> Array.toList
