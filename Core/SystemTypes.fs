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
type GrainIdentityI =
    private GrainIdentity of IGrainWithIntegerKey
    with
        static member create (ref: IGrainWithIntegerKey) = ref.AsReference<IGrainWithIntegerKey>() |> GrainIdentity
        member me.key = let (GrainIdentity ref) = me in ref.GetPrimaryKeyLong()

// Mandatory input to all grain functions, also with different types
// for different grain key types.
type GrainFunctionInputI<'TServices> = { 
    Identity: GrainIdentityI
    Services: 'TServices
    GrainFactory: IGrainFactory
    }

// Extract method info of native F# function from a quotation containing a call to it
let getMethod = function
    | Call (_, mi, _) -> [], mi
    | Lambdas (vs, Call(_, mi, _)) -> List.map (fun (v: Var list) -> (List.head v).Type) vs, mi
    | _ -> failwith "Not a function call"

// Cache of all grain module functions
module __GrainFunctionCache =
    let internal methodCacheI = Dictionary<MethodInfo, (IGrainFactory * int64 -> IGrainWithIntegerKey) * MethodInfo>()

    [<Obsolete("For internal use only; do not use directly.")>]
    let __registeri (func: Expr, factory: IGrainFactory * int64 -> IGrainWithIntegerKey, interfaceMethod: MethodInfo) =
        let (_, funcmi) = getMethod func
        methodCacheI.Add(funcmi, (factory, interfaceMethod))

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
    // Proxy generator. Operates in a completely type-safe manner. Takes a quotation
    // containing a method call and finds the corresponding interface/method pair.
    // Also builds a proxy with the same argument types and returns it.
    // TODO: This could probably be sped up with some caching, since `calln` instances
    // are immutable and can be cached.
    member me.proxyi (f: Expr<GrainFunctionInputI<_> -> 'tres>) (key: int64) : 'tres =
        let (types, mi) = getMethod f
        let (refFactory, interfaceMethod) = __GrainFunctionCache.methodCacheI.[mi]
        let ref = refFactory (me, key)

        match types with
        | [p] ->
            let t = typedefof<ProxyFunctions.call1<_,_>>.MakeGenericType([|p; mi.ReturnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | [p1; p2] ->
            let t = typedefof<ProxyFunctions.call2<_,_,_>>.MakeGenericType([|p1; p2; mi.ReturnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | _ -> failwith "Too many args XD" // We'll add more of those callers and support varying numbers of arguments here
