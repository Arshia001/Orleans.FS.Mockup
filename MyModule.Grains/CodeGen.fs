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
   Grain classes are generated for each grain module. These classes get services
   from the DI system, and compose them into a GrainFunctionInput which will be fed
   into grain functions along with any additional parameters.
*)
type HelloWorkerGrainImpl() as me =
    inherit Grain()

    let i = { Identity = GrainIdentityI.create me; Services = (); GrainFactory = me.GrainFactory }

    interface IHelloWorkerGrain with
        member __.SayHello name = HelloWorkerGrain.sayHello i name

type HelloGrainImpl(_persistentState: IPersistentState<HelloGrainState>, _transientState: ITransientState<HelloArgs.T>) as me =
    inherit Grain()

    let i = { Identity = GrainIdentityI.create me; Services = { persistentState = _persistentState; transientState = _transientState }; GrainFactory = me.GrainFactory }

    interface IHelloGrain with
        member __.SetName name = HelloGrain.setName i name
        member __.SayHello () = HelloGrain.sayHello i

(*
   This is where user code and generated code come together. A cache of all grain
   functions and their corresponding interface/method pair is built. This cache is
   used by the proxy generators to translate function calls into grain interface
   calls.
*)
#nowarn "44"
module private __GrainInit =
    let helloWorkerFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloWorkerGrain>(key) :> IGrainWithIntegerKey
    __GrainFunctionCache.__registeri (<@@ HelloWorkerGrain.sayHello @@>, helloWorkerFactory, typeof<IHelloWorkerGrain>.GetMethod("SayHello"))

    let helloFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloGrain>(key) :> IGrainWithIntegerKey
    __GrainFunctionCache.__registeri (<@@ HelloGrain.sayHello @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SayHello"))
    __GrainFunctionCache.__registeri (<@@ HelloGrain.setName @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SetName"))
