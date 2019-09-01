[<Core.SystemTypes.GrainModuleWithIntegerKey()>]
module MyModule.Grains.HelloWorkerGrain

open HelloArgs
open Core.SystemTypes
open System.Threading.Tasks

// See comments in HelloGrain.fs.

let sayHello i name = sprintf "Hello %s from worker grain %i" (getName name) i.IdentityI.key |> Task.FromResult
