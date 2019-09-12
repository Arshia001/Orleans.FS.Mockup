[<Core.SystemTypes.GrainModuleWithIntegerKey([| "'T1" |])>]
module MyModule.Grains.HelloWorkerGrain

open HelloArgs
open Core.SystemTypes
open System.Threading.Tasks

// See comments in HelloGrain.fs.

let sayHello<'T1, 'T2> (i: GrainFunctionInputI<unit>) name (t1: 'T1) (t2: 'T2) = 
    sprintf "Hello %s from worker grain %i with class generic argument %O and method generic argument %O"
        (getName name) i.IdentityI.key t1 t2 |> Task.FromResult
