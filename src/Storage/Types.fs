namespace MF.Storage

type Key = Key of string

type GetKey<'Item> = 'Item -> Key

type Storage<'Item> = {
    GetKey: GetKey<'Item>
    Serialize: 'Item -> string
    Parse: string -> 'Item option
    Save: 'Item list -> unit
    Load: unit -> 'Item list
    Clear: unit -> unit
    Title: string
}
