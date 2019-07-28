namespace MyModule.Grains

open Orleans
open Orleans.Core
open Orleans.Runtime
open SystemTypes
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open HelloGrain
open HelloWorkerGrain

// All of this will be coded by hand.

[<CLIMutable>]
type HelloGrainState = { lastHello: string }

type Services = { persistentState: IPersistentState<HelloGrainState>; transientState: ITransientState<HelloArgs.T> }

// Functions inside a `GrainModule` type will be grouped into a single grain class of the same
// name as the type.
type HelloGrain(i: GrainFunctionInput<Services>) =
    interface IGrainModuleWithIntegerKey

    member __.SetName name =
        i.Services.transientState.Value <- name
        Task.CompletedTask

    (*
       TODO should we switch to F# async instead of tasks? I don't think so. On one hand, Orleans
       already uses tasks, so it'll just be extra overhead. For another, I remember seeing a
       discussion about supporting a state machine-style task computation expression in the F#
       language repo, so when that's released, we'll definitely want to stick to tasks.
    *)
    member __.SayHello () = task {
        let! result = Grain.invokeir i (i.Identity.longKey ()) <| fun (h: HelloWorkerGrain) -> h.SayHello(i.Services.transientState.Value)
        i.Services.persistentState.State <- { lastHello = result }
        do! i.Services.persistentState.WriteStateAsync()
        return result
    }
