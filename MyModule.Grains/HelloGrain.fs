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

// Functions inside a `GrainModule` will be grouped into a single grain class of the same
// name as the parent module.
[<GrainModuleWithIntegerKey(typeof<Services>)>]
module HelloGrain =
    let SetName i name =
        i.Services.transientState.Value <- name
        Task.CompletedTask

    (*
       TODO should we switch to F# async instead of tasks? I don't think so. On one hand, Orleans
       already uses tasks, so it'll just be extra overhead. For another, I remember seeing a
       discussion about supporting a state machine-style task computation expression in the F#
       language repo, so when that's released, we'll definitely want to stick to tasks.
    *)
    let SayHello i = task {
        let! result = Grain.proxyi <@ HelloWorkerGrain.SayHello @> i.GrainFactory i.Identity.key (i.Services.transientState.Value)
        i.Services.persistentState.State <- { lastHello = result }
        do! i.Services.persistentState.WriteStateAsync()
        return result
    }
