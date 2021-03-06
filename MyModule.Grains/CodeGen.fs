﻿namespace MyModule.Grains

(* 
   All code in this file is codegen'ed.

   It is *very* important that other code in this library not depend on the contents
   of this file or any code in the interfaces library. This is because we'll have two
   builds (similar to what Orleans already does for C# projects): the first will be
   made with just the user code, from which metadata will be extracted. That metadata
   will then be used to add the generated code in so we can make the final build.

   This is also why this file was added at the bottom of the project.
*)

open System.Threading.Tasks
open Orleans.Runtime
open Orleans
open Core.SystemTypes
open MyModule.Grains.Interfaces
open Orleans.Concurrency

(*
   This is where user code and generated code come together. A cache of all grain
   functions and their corresponding interface/method pair is built. This cache is
   used by the proxy generators to translate function calls into grain interface
   calls.
*)
#nowarn "44"
module private __GrainInit =
    let private helloWorkerFactory_int (factory: IGrainFactory, key) = factory.GetGrain<IHelloWorkerGrain<int>>(key) :> IGrainWithIntegerKey
    let mi = typeof<IHelloWorkerGrain<int>>.GetMethod("SayHello").MakeGenericMethod(typeof<string>)
    // All FSharpFuncs will be discovered at code-gen time and corresponding calls will be created here.
    // See bottom of file for method by which the corresponding method info can be discovered from an FSFunc.
    __GrainFunctionCache.__registeri (
        System.Type.GetType("MyModule.Grains.HelloGrain+sayHelloProxy@58"),
        helloWorkerFactory_int, fun grn -> __ProxyFunctions.call3<HelloArgs.T, int, string, Task<string>>(grn, mi, []) |> box)

    // No one invoked the hello grain methods from within this assembly; so no codegen happens for those.

    // This is called so the static constructor runs and method info caches are built
    let ensureInitialized () = ()

(*
   Grain classes are generated for each grain module. These classes get services
   from the DI system, and compose them into a GrainFunctionInput which will be fed
   into grain functions along with any additional parameters.
*)
[<StatelessWorker>]
type HelloWorkerGrainImpl<'T1>() =
    inherit FSharpGrain()

    static do __GrainInit.ensureInitialized ()

    member val i = Unchecked.defaultof<InputI<HelloWorkerGrainT<'T1>>> with get, set

    override me.OnActivateAsync () =
        me.i <- { IdentityI = me |> GrainIdentityI.create; Record = HelloWorkerGrainT (); GrainFactory = me.GrainFactory }
        Task.CompletedTask

    interface IHelloWorkerGrain<'T1> with
        member me.SayHello name (t1: 'T1) (t2: 'T2) = HelloWorkerGrain.sayHello me.i name t1 t2

type HelloGrainImpl(
                    [<PersistentState("state")>] _persistentState: IPersistentState<HelloGrainState>, 
                    _transientState: ITransientState<HelloArgs.T>) =
    inherit FSharpGrain()

    static do __GrainInit.ensureInitialized ()

    member val i = Unchecked.defaultof<InputI<HelloGrainT>> with get, set

    override me.OnActivateAsync () = 
        me.i <- { IdentityI = me |> GrainIdentityI.create; Record = { persistentState = _persistentState; transientState = _transientState }; GrainFactory = me.GrainFactory }
        HelloGrain.onActivate me.i

    override me.OnDeactivateAsync () =
        HelloGrain.onDeactivate me.i

    interface IRemindable with
        member me.ReceiveReminder (n, s) = HelloGrain.receiveReminder (me.i, n, s)

    interface IHelloGrain with
        member me.SetName name = HelloGrain.setName me.i name
        member me.SayHello () = HelloGrain.sayHello me.i ()


// To discover MethodInfo from FSFunc:

// open Mono.Reflection

//let getInvokeMethod f = // TODO traverse all fsharp funcs for more than 5 arguments
//    f.GetType().GetMethods()
//    |> Array.filter (fun m -> m.Name = "Invoke")
//    |> Array.maxBy (fun m -> m.GetParameters().Length)

//let getMethodFromBody (body: MethodInfo) =
//    body.GetInstructions()
//    |> Seq.filter (fun i -> i.OpCode = Emit.OpCodes.Call)
//    |> Seq.map (fun i -> i.Operand :?> MethodInfo)
//    |> Seq.head
