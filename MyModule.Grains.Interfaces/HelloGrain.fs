module HelloGrain

open Orleans
open System.Threading.Tasks

(*
   This entire library will be codegen'ed. It'll provide two
   interfaces to the grains: One will be a normal CLR interface
   usable from other languages, and the other will be an F# wrapper
   over that interface.
*)

// This is the normal interface...
type IHelloGrain =
    inherit IGrainWithIntegerKey

    abstract SetName: name: HelloArgs.T -> Task
    abstract SayHello: unit -> Task<string>

// ... and these are the F# wrapper functions.

(*
   Grain factory *could* get F#-specific extension methods to create
   grain references.
*)
type IGrainFactory with
    member this.getHelloGrain id = this.GetGrain<IHelloGrain>(id)

let SetName name (grainRef: IHelloGrain) = grainRef.SetName(name)

let SayHello (grainRef: IHelloGrain) = grainRef.SayHello()
