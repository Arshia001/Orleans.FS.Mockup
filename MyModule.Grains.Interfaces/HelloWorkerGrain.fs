module HelloWorkerGrain

open Orleans
open System.Threading.Tasks

// See comments in HelloGrain.fs.

type IHelloWorkerGrain =
    inherit IGrainWithIntegerKey

    abstract SayHello: name: HelloArgs.T -> Task<string>

type IGrainFactory with
    member this.getHelloWorkerGrain id = this.GetGrain<IHelloWorkerGrain>(id)

let SayHello name (grainRef: IHelloWorkerGrain) = grainRef.SayHello(name)
