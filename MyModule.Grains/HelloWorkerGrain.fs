[<SystemTypes.GrainModuleWithIntegerKey()>]
module MyModule.Grains.HelloWorkerGrain

open HelloArgs
open SystemTypes
open System.Threading.Tasks

// See comments in HelloGrain.fs.

let SayHello i name = sprintf "Hello %s from worker grain %i" (getName name) i.Identity.key |> Task.FromResult
