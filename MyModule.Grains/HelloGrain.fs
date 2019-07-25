module HelloGrain

open Orleans
open Orleans.Core
open Orleans.Runtime
open SystemTypes
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open HelloGrain

// All of this will be coded by hand.

[<CLIMutable>]
type State = { lastHello: string }

type Services = { persistentState: IPersistentState<State>; transientState: ITransientState<HelloArgs.T> }

// TODO: What about generic grains? Is it possible and/or necessary to support them?

// Functions inside a `GrainModule` will be grouped into a single grain class of the same
// name as the parent module.
[<GrainModuleWithIntegerKey(typeof<Services>)>]
module Functions =
    let SetName i name =
        i.Services.transientState.Value <- name
        Task.CompletedTask

    let SayHello i = task {
        let helloWorker = i.Identity.longKey () |> getHelloWorker // See comments on getHelloWorker
        let! result = helloWorker.SayHello(i.Services.transientState.Value)
        i.Services.persistentState.State <- { lastHello = result }
        do! i.Services.persistentState.WriteStateAsync()
        return result
    }

// Anything below here will be codegen'ed.

type HelloGrainImpl(_persistentState: IPersistentState<State>, _transientState: ITransientState<HelloArgs.T>) as me =
    inherit Grain()

    let i = { Identity = GrainIdentity.create me; Services = { persistentState = _persistentState; transientState = _transientState } }

    interface IHelloGrain with
        member __.SetName name = Functions.SetName i name
        member __.SayHello() = Functions.SayHello i