namespace MyModule.Grains

// All code in this file is codegen'ed.

open System.Threading.Tasks
open Orleans.Runtime
open Orleans
open SystemTypes
open MyModule.Grains.Interfaces

type HelloWorkerGrainImpl() as me =
    inherit Grain()

    let instance = HelloWorkerGrain({ Identity = GrainIdentity.create me; Services = (); GrainFactory = me.GrainFactory })

    interface IHelloWorkerGrain with
        member __.SayHello name = instance.SayHello name

        member __.InvokeFunc f = 
            let f = f :?> HelloWorkerGrain -> Task
            f instance
        member __.InvokeFuncWithResult f = 
            let f = f :?> HelloWorkerGrain -> Task<obj>
            f instance

type HelloGrainImpl(_persistentState: IPersistentState<HelloGrainState>, _transientState: ITransientState<HelloArgs.T>) as me =
    inherit Grain()

    let instance = HelloGrain({ Identity = GrainIdentity.create me; Services = { persistentState = _persistentState; transientState = _transientState }; GrainFactory = me.GrainFactory })

    interface IHelloGrain with
        member __.SetName name = instance.SetName name
        member __.SayHello () = instance.SayHello ()

        member __.InvokeFunc f = 
            let f = f :?> HelloGrain -> Task
            f instance
        member __.InvokeFuncWithResult f = 
            let f = f :?> HelloGrain -> Task<obj>
            f instance

module private __GrainInit =
    registerLong<IHelloWorkerGrain> typeof<HelloWorkerGrain>
    registerLong<IHelloGrain> typeof<HelloGrain>