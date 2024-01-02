// ========================================================================================================
// === F# / Project fake build ==================================================================== 1.3.0 =
// --------------------------------------------------------------------------------------------------------
// Options:
//  - no-clean   - disables clean of dirs in the first step (required on CI)
//  - no-lint    - lint will be executed, but the result is not validated
// ========================================================================================================

open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

open ProjectBuild
open Utils

[<EntryPoint>]
let main args =
    args |> Args.init

    Targets.init {
        Project = {
            Name = "GuildWars2 Console"
            Summary = "Console application for help a crafting process in Guild Wars 2"
            Git = Git.init ()
        }
        Specs =
            Spec.defaultConsoleApplication [
                OSX
                Windows
                Linux
            ]
    }

    args |> Args.run
