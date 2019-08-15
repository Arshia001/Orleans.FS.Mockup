module SystemTypes

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
    abstract Value: 't with get, set

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

module __GrainPrivate =
    let internal methodsi = Dictionary<MethodInfo, (IGrainFactory * int64 -> IGrainWithIntegerKey) * MethodInfo>()

    let __registeri (func: Expr, factory: IGrainFactory * int64 -> IGrainWithIntegerKey, interfaceMethod: MethodInfo) =
        let (_, funcmi) = getMethod func
        methodsi.Add(funcmi, (factory, interfaceMethod))

module __ProxyFunctions =
    type call1<'p1, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, 'res>()
        override __.Invoke(x: 'p1) = method.Invoke(grainRef, (x :> obj) :: args |> List.rev |> List.toArray) :?> 'res

    type call2<'p1, 'p2, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, FSharpFunc<'p2, 'res>>()
        override __.Invoke(x: 'p1) = call1<'p2, 'res>(grainRef, method, x :> obj :: args) |> box |> unbox<FSharpFunc<'p2, 'res>>

module Grain =
    let proxyi (f: Expr<GrainFunctionInputI<_> -> 'tres>) (factory: IGrainFactory) (key: int64) : 'tres =
        let (types, mi) = getMethod f
        let (refFactory, interfaceMethod) = __GrainPrivate.methodsi.[mi]
        let ref = refFactory (factory, key)

        match types with
        | [p] ->
            let t = typedefof<__ProxyFunctions.call1<_,_>>.MakeGenericType([|p; mi.ReturnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | [p1; p2] ->
            let t = typedefof<__ProxyFunctions.call2<_,_,_>>.MakeGenericType([|p1; p2; mi.ReturnType|])
            t.GetConstructors().[0].Invoke([|ref; interfaceMethod; []|]) |> unbox<'tres>
        | _ -> failwith "Too many types? XD" //??
