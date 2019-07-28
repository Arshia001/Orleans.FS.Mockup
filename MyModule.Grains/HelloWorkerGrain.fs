namespace MyModule.Grains

open HelloArgs
open HelloWorkerGrain
open SystemTypes
open System.Threading.Tasks
open Orleans

// See comments in HelloGrain.fs.

type HelloWorkerGrain(i: GrainFunctionInput<unit>) =
    interface IGrainModuleWithIntegerKey

    member __.SayHello name = sprintf "Hello %s from worker grain %i" (getName name) (i.Identity.longKey ()) |> Task.FromResult
