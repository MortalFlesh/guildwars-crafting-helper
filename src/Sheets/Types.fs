namespace Sheets

type UpdateData = {
    spreadsheetId: string
    listName: string
    data: (string * float list list) list //Map<string, float list list>
}
