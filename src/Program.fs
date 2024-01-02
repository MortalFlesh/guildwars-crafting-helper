open System
open System.IO
open MF.ConsoleApplication
open MF.GuildWars.Console
open MF.ErrorHandling
open MF.Utils

[<EntryPoint>]
let main argv =
    consoleApplication {
        title "GuildWars 2"
        info ApplicationInfo.MainTitle
        version AssemblyVersionInformation.AssemblyVersion

        command "gw:check" {
            Description = "Check for resources based on checklist(s)."
            Help = None
            Arguments = [
                Argument.requiredArray "checklist" "A list of checklist(s)."
            ]
            Options = [
                Option.optional "config" (Some "c") "A file with a configuration." (Some ".gw.json")
            ]
            Initialize = None
            Interact = None
            Execute = Command.Check.execute
        }

        command "gw:bank" {
            Description = "Inspect a bank for all items and their prices."
            Help = None
            Arguments = []
            Options = [
                Option.optional "config" (Some "c") "A file with a configuration." (Some ".gw.json")
            ]
            Initialize = None
            Interact = None
            Execute = Command.Bank.execute
        }

        command "gw:characters" {
            Description = "Inspect all characters for equipment and inventories."
            Help = None
            Arguments = [
                Argument.optionalArray "characters" "A list of characters (part of the name) which you want to update. If none given, all of them will be updated." (Some [])
            ]
            Options = [
                Option.optional "config" (Some "c") "A file with a configuration." (Some ".gw.json")
            ]
            Initialize = None
            Interact = None
            Execute = Command.Characters.execute
        }
    }
    |> run argv
