open System
open Orleans
open Orleans.Hosting
open FSharp.Control.Tasks.V2
open MyModule.GrainProxies
open Microsoft.Extensions.DependencyInjection
open Core.SystemTypes
open System.Threading.Tasks

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
        
        let client = host.Services.GetRequiredService<IClusterClient>()
        
        let hello = client.getHelloGrain 0L
        do! hello |> HelloGrain.setName (HelloArgs.create "world")
        let! result = hello |> HelloGrain.sayHello

        printfn "Run result: %s" result

        do! Task.Delay(2_000) // Wait for the timer inside HelloGrain to fire

        do! host.StopAsync()
    } |> Async.AwaitTask |> Async.RunSynchronously

    0
