namespace MyModule.Grains

open Orleans
open Orleans.Runtime
open Core.SystemTypes
open System.Threading.Tasks
open FSharp.Control.Tasks.V2

// All of this will be coded by hand.

[<CLIMutable>]
type HelloGrainState = { lastHello: string }

type Services = { 
    persistentState: IPersistentState<HelloGrainState>
    transientState: ITransientState<HelloArgs.T>
}

// Functions inside a `GrainModule` will be grouped into a single grain class of the same
// name as the parent module.
[<GrainModuleWithIntegerKey(typeof<Services>)>]
module HelloGrain =
    (*
       `i` will be constrained to be of type GrainFunctionInputI (with the I corresponding to 
       'integer key') by the codegen tool. We can make type-safe access to the grain identity
       this way.
    *)
    let setName i name =
        i.Services.transientState.Value <- Some name
        Task.CompletedTask

    (*
       TODO should we switch to F# async instead of tasks? I don't think so. On one hand, Orleans
       already uses tasks, so it'll just be extra overhead. For another, I remember seeing a
       discussion about supporting a state machine-style task computation expression in the F#
       language repo, so when that's released, we'll definitely want to stick to tasks.
    *)
    let sayHello i () = task {
        // We generate a proxy by giving it a grain function and the grain key.
        // GrainFunctionInputI has the added benefit of making the keys fed into
        // proxies type-safe as well, hence the `i` in `proxyi`.
        // This gives us a proxy function with the same arguments as the grain function...
        let sayHelloProxy = i.GrainFactory.invokei <@ HelloWorkerGrain.sayHello @> (i.Identity.key + 42L)
        
        match i.Services.transientState.Value with
        | Some name ->
            // ... which can be used normally.
            let! result = sayHelloProxy name
        
            // Since the actor model keeps each actor's data inside it, mutability is not only
            // harmless (in the concurrency sense), but actually required to make everything work.
            i.Services.persistentState.State <- { lastHello = result }
            do! i.Services.persistentState.WriteStateAsync()

            return result

        | None -> return "I have no one to say hello to :("
    }
