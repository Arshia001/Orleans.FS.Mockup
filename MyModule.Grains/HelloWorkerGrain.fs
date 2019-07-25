module HelloWorkerGrain

open HelloArgs
open HelloWorkerGrain
open SystemTypes
open System.Threading.Tasks
open Orleans

// See comments in HelloGrain.fs.

[<GrainModuleWithIntegerKey>]
module Functions =
    let SayHello i name = sprintf "Hello %s from worker grain %i" (getName name) (i.Identity.longKey ()) |> Task.FromResult

type HelloWorkerGrainImpl() as me =
    inherit Grain()

    let i = { Identity = GrainIdentity.create me; Services = () }

    interface IHelloWorkerGrain with
        member __.SayHello name = Functions.SayHello i name