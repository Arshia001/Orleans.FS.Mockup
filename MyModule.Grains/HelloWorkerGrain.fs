namespace MyModule.Grains

open HelloArgs
open Core.SystemTypes
open System.Threading.Tasks
open Orleans.Concurrency

// See comments in HelloGrain.fs.

// Since empty records are not allowed, we'll use a single-case union for the
// special case of grains with no data and no services
type HelloWorkerGrainT<'T1> = HelloWorkerGrainT of unit

[<GrainModuleWithIntegerKey(typedefof<HelloWorkerGrainT<_>>); StatelessWorker>]
module HelloWorkerGrain =
    let sayHello<'T1, 'T2> (i: InputI<HelloWorkerGrainT<'T1>>) name (t1: 'T1) (t2: 'T2) = 
        sprintf "Hello %s from worker grain %i with class generic argument %O and method generic argument %O"
            (getName name) i.IdentityI.key t1 t2 |> Task.FromResult
