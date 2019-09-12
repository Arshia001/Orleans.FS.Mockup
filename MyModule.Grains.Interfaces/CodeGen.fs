(*
   This entire library will be codegen'ed. It'll provide two
   interfaces to the grains: One will be a normal CLR interface
   usable from other languages, and the other will be an F# wrapper
   over that interface.
*)

namespace MyModule.Grains.Interfaces
    open Orleans
    open System.Threading.Tasks

    // This is the normal interface, in an `Interfaces` namespace...
    type IHelloWorkerGrain<'T1> =
        inherit IGrainWithIntegerKey

        abstract SayHello: name: HelloArgs.T -> t1: 'T1 -> t2: 'T2 -> Task<string>

    type IHelloGrain =
        inherit IGrainWithIntegerKey

        abstract SetName: name: HelloArgs.T -> Task
        abstract SayHello: unit -> Task<string>

namespace MyModule.GrainProxies
    open Orleans
    open MyModule.Grains.Interfaces

    // ... and these are the F# wrapper functions in a `Proxies` namespace (name up for debate).
    
    (*
       Grain factory *could* get F#-specific extension methods to create
       grain references.
    *)
    [<AutoOpen>]
    module GrainFactory =
        type IGrainFactory with
            member this.getHelloGrain id = this.GetGrain<IHelloGrain>(id)
            member this.getHelloWorkerGrain<'T1> id = this.GetGrain<IHelloWorkerGrain<'T1>>(id)

    (*
       One might argue these functions are useless. However, they can be
       used for function chaining (`getGrain id |> doSomething`) so I'd
       rather they be generated anyway.
    *)
    module HelloWorkerGrain =
        let sayHello name t1 t2 (grainRef: IHelloWorkerGrain<'T1>) = grainRef.SayHello(name) t1 t2

    module HelloGrain =
        let setName name (grainRef: IHelloGrain) = grainRef.SetName(name)
        let sayHello (grainRef: IHelloGrain) = grainRef.SayHello()
