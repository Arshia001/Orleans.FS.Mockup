module Core.SystemTypes

open System
open Orleans
open System.Threading.Tasks
open System.Collections.Immutable
open Microsoft.FSharp.Quotations
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

// Everything in this file belongs inside Orleans.

// Similar to IPersistentState, only this one just holds a piece of data
// between calls, no persisting included.
[<Interface>]
type ITransientState<'t> =
    abstract Value: 't option with get, set

type TransientState<'t>() =
    interface ITransientState<'t> with
        member val Value = Option<'t>.None with get, set

// This attribute will be discovered at codegen time. Fields inside the
// `services` record type will be added to the grain's constructor for DI.
[<AbstractClass>]
type GrainModuleAttribute(services: Type) = inherit Attribute()

[<AttributeUsage(AttributeTargets.Class)>]
type GrainModuleWithIntegerKey(services: Type) =
    inherit GrainModuleAttribute(services)
    new() = GrainModuleWithIntegerKey(typeof<unit>)

// A wrapper around IGrain. Passing a clear IGrain into grain functions will
// give people the wrong idea about what they can do with it.
// We'll have different identity types for grains with different key types.
type GrainIdentityI<'IGrain when 'IGrain :> IGrainWithIntegerKey> =
    private GrainIdentity of 'IGrain
    with
        static member create (ref: 'IGrain) = ref.AsReference<'IGrain>() |> GrainIdentity
        member me.key = let (GrainIdentity ref) = me in ref.GetPrimaryKeyLong()

// Mandatory input to all grain functions, also with different types
// for different grain key types.
type GrainFunctionInputI<'Services, 'IGrain when 'IGrain :> IGrainWithIntegerKey> = { 
    Identity: GrainIdentityI<'IGrain>
    Services: 'Services
    GrainFactory: IGrainFactory
    }

open Mono.Reflection

// TODO traverse all fsharp funcs for more than 5 arguments
let getInvokeMethod f =
    f.GetType().GetMethods()
    |> Array.filter (fun m -> m.Name = "Invoke")
    |> Array.maxBy (fun m -> m.GetParameters().Length)

let getMethodFromBody (body: MethodInfo) =
    body.GetInstructions()
    |> Seq.filter (fun i -> i.OpCode = Emit.OpCodes.Call)
    |> Seq.map (fun i -> i.Operand :?> MethodInfo)
    |> Seq.head// Cache of all grain module functions

module __GrainFunctionCache =
    let internal methodCacheI = Dictionary<string, (IGrainFactory * int64 -> IGrainWithIntegerKey) * (MethodInfo * Type list * Type)>()

    [<Obsolete("For internal use only; do not use directly.")>]
    let __registeri (fullTypeName: string, 
                     factory: IGrainFactory * int64 -> IGrainWithIntegerKey,
                     method: MethodInfo * Type list * Type) =
        methodCacheI.Add(fullTypeName, (factory, method))

    let internal getMethodAndTypes (f: FSharpFunc<_,_>) =
        let name = f.GetType().FullName
        match methodCacheI.TryGetValue name with
        | true, x -> x
        | false, _ ->  failwithf "Unknown function %s" name

// Implementation of proxies. Proxies are responsible for collecting
// all arguments and feeding them into interface functions at once.
module internal ProxyFunctions =
    type call1<'p1, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, 'res>()
        override __.Invoke(x: 'p1) = method.Invoke(grainRef, (x :> obj) :: args |> List.rev |> List.toArray) :?> 'res

    type call2<'p1, 'p2, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, FSharpFunc<'p2, 'res>>()
        override __.Invoke(x: 'p1) = call1<'p2, 'res>(grainRef, method, x :> obj :: args) |> box |> unbox<FSharpFunc<'p2, 'res>>

type IGrainFactory with
    // Proxy generator. Operates in a completely type-safe manner. Takes a function
    // and finds the corresponding interface/method pair.
    // Also builds a proxy with the same argument types and returns it.
    // TODO: This could probably be sped up with some caching, since `calln` instances
    // are immutable and can be cached.
    member me.invokei (f: GrainFunctionInputI<_,_> -> 'tres) (key: int64) : 'tres =
        let (refFactory, (interfaceMethod, types, returnType)) = __GrainFunctionCache.getMethodAndTypes f
        let ref = refFactory (me, key)

        match types with
        | [] ->
            let t = typedefof<ProxyFunctions.call1<_,_>>.MakeGenericType([|typeof<unit>; returnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | [p] ->
            let t = typedefof<ProxyFunctions.call1<_,_>>.MakeGenericType([|p; returnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | [p1; p2] ->
            let t = typedefof<ProxyFunctions.call2<_,_,_>>.MakeGenericType([|p1; p2; returnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | _ -> failwith "Too many args XD" // We'll add more of those callers and support varying numbers of arguments here
