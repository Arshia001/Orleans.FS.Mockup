open System
open Orleans
open Orleans.Hosting
open FSharp.Control.Tasks.V2
open MyModule.Grains

[<EntryPoint>]
let main _argv =
    let host =
        SiloHostBuilder()
            .UseLocalhostClustering()
            .ConfigureApplicationParts(fun p -> 
                p.AddApplicationPart(typeof<HelloGrainState>.Assembly).WithReferences()
                    .WithCodeGeneration()
                    |> ignore
                )
            .AddMemoryGrainStorageAsDefault()
            .Build()
    
    task {
        do! host.StartAsync()
        
        let client = host.Services.GetService(typeof<IClusterClient>) :?> IClusterClient
        
        let hello = client.getHelloGrain 0L
        do! HelloGrain.setName (HelloArgs.create "world") hello
        let! result = HelloGrain.sayHello hello

        printfn "Run result: %s" result
    } |> Async.AwaitTask |> Async.RunSynchronously

    0
