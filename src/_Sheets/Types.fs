namespace Sheets

type UpdateFloatData = {
    spreadsheetId: string
    listName: string
    data: (string * float list list) list //Map<string, float list list>
}

type UpdateStringData = {
    spreadsheetId: string
    listName: string
    data: (string * string list list) list //Map<string, string list list>
}

[<RequireQualifiedAccess>]
type UpdateData =
    | Float of UpdateFloatData
    | String of UpdateStringData
