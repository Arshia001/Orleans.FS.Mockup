open System
open Orleans
open Orleans.Hosting
open HelloGrain
open FSharp.Control.Tasks.V2

[<EntryPoint>]
let main _argv =
    let host =
        SiloHostBuilder()
            .UseLocalhostClustering()
            .ConfigureApplicationParts(fun p -> 
                p.AddApplicationPart(typeof<HelloGrain.State>.Assembly).WithReferences()
                    .WithCodeGeneration()
                    |> ignore
                )
            .AddMemoryGrainStorageAsDefault()
            .Build()
    
    task {
        do! host.StartAsync()
        
        let client = host.Services.GetService(typeof<IClusterClient>) :?> IClusterClient
        
        let hello = client.getHelloGrain 0L
        do! SetName (HelloArgs.create "world") hello
        let! result = SayHello hello

        printfn "Run result: %s" result
    } |> Async.AwaitTask |> Async.RunSynchronously

    0
