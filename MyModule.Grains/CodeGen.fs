namespace MyModule.Grains

// All code in this file is codegen'ed.

open System.Threading.Tasks
open Orleans.Runtime
open Orleans
open SystemTypes
open MyModule.Grains.Interfaces

type HelloWorkerGrainImpl() as me =
    inherit Grain()

    let i = { Identity = GrainIdentityI.create me; Services = (); GrainFactory = me.GrainFactory }

    interface IHelloWorkerGrain with
        member __.SayHello name = HelloWorkerGrain.SayHello i name

type HelloGrainImpl(_persistentState: IPersistentState<HelloGrainState>, _transientState: ITransientState<HelloArgs.T>) as me =
    inherit Grain()

    let i = { Identity = GrainIdentityI.create me; Services = { persistentState = _persistentState; transientState = _transientState }; GrainFactory = me.GrainFactory }

    interface IHelloGrain with
        member __.SetName name = HelloGrain.SetName i name
        member __.SayHello () = HelloGrain.SayHello i

module private __GrainInit =
    let helloWorkerFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloWorkerGrain>(key) :> IGrainWithIntegerKey
    __GrainPrivate.__registeri (<@@ HelloWorkerGrain.SayHello @@>, helloWorkerFactory, typeof<IHelloWorkerGrain>.GetMethod("SayHello"))

    let helloFactory (factory: IGrainFactory, key) = factory.GetGrain<IHelloGrain>(key) :> IGrainWithIntegerKey
    __GrainPrivate.__registeri (<@@ HelloGrain.SayHello @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SayHello"))
    __GrainPrivate.__registeri (<@@ HelloGrain.SetName @@>, helloFactory, typeof<IHelloGrain>.GetMethod("SetName"))
