namespace MyModule.Grains

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

(*
   This is where user code and generated code come together. A cache of all grain
   functions and their corresponding interface/method pair is built. This cache is
   used by the proxy generators to translate function calls into grain interface
   calls.
*)
#nowarn "44"
module private __GrainInit =
    let private helloWorkerFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloWorkerGrain>(key) :> IGrainWithIntegerKey
    __GrainFunctionCache.__registeri (<@@ HelloWorkerGrain.sayHello @@>, helloWorkerFactory, typeof<IHelloWorkerGrain>.GetMethod("SayHello"))

    let private helloFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloGrain>(key) :> IGrainWithIntegerKey
    __GrainFunctionCache.__registeri (<@@ HelloGrain.sayHello @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SayHello"))
    __GrainFunctionCache.__registeri (<@@ HelloGrain.setName @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SetName"))

    // This is called so the static constructor runs and method info caches are built
    let ensureInitialized () = ()

(*
   Grain classes are generated for each grain module. These classes get services
   from the DI system, and compose them into a GrainFunctionInput which will be fed
   into grain functions along with any additional parameters.
*)
type HelloWorkerGrainImpl() =
    inherit Grain()

    static do __GrainInit.ensureInitialized ()

    member val i = Unchecked.defaultof<GrainFunctionInputI<unit, IHelloWorkerGrain>> with get, set

    override me.OnActivateAsync () =
        me.i <- { Identity = me :> IHelloWorkerGrain |> GrainIdentityI.create; Services = (); GrainFactory = me.GrainFactory }
        base.OnActivateAsync()

    interface IHelloWorkerGrain with
        member me.SayHello name = HelloWorkerGrain.sayHello me.i name

type HelloGrainImpl(
                    [<PersistentState("state")>] _persistentState: IPersistentState<HelloGrainState>, 
                    _transientState: ITransientState<HelloArgs.T>) =
    inherit Grain()

    static do __GrainInit.ensureInitialized ()

    member val i = Unchecked.defaultof<GrainFunctionInputI<Services, IHelloGrain>> with get, set

    override me.OnActivateAsync () =
        me.i <- { Identity = me :> IHelloGrain |> GrainIdentityI.create; Services = { persistentState = _persistentState; transientState = _transientState }; GrainFactory = me.GrainFactory }
        base.OnActivateAsync()

    interface IHelloGrain with
        member me.SetName name = HelloGrain.setName me.i name
        member me.SayHello () = HelloGrain.sayHello me.i
