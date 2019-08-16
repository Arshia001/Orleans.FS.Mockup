open System
open Orleans
open Orleans.Hosting
open FSharp.Control.Tasks.V2
open MyModule.GrainProxies
open Microsoft.Extensions.DependencyInjection
open Core.SystemTypes

[<EntryPoint>]
let main _argv =
    let host =
        SiloHostBuilder()
            .UseLocalhostClustering()
            .ConfigureApplicationParts(fun p -> 
                p.AddApplicationPart(typeof<MyModule.Grains.HelloGrainState>.Assembly).WithReferences()
                    .WithCodeGeneration()
                    |> ignore
                )
            .AddMemoryGrainStorageAsDefault()
            .ConfigureServices(fun s -> s.AddTransient(typedefof<ITransientState<_>>, typedefof<TransientState<_>>) |> ignore)
            .Build()
    
    task {
        do! host.StartAsync()
        
        let client = host.Services.GetService(typeof<IClusterClient>) :?> IClusterClient
        
        let hello = client.getHelloGrain 0L
        do! hello |> HelloGrain.setName (HelloArgs.create "world")
        let! result = hello |> HelloGrain.sayHello

        printfn "Run result: %s" result
    } |> Async.AwaitTask |> Async.RunSynchronously

    0
