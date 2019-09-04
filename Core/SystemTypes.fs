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

(*
   While grains have a base class (likely to change soon), we'll use these
   attributes on functions to introduce them to the code generator. Corresponding
   methods will the be generated on the grain class.
*)
[<AttributeUsage(AttributeTargets.Method)>]
type OnActivateAttribute() = inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type OnDeactivateAttribute() = inherit Attribute()

[<AttributeUsage(AttributeTargets.Method)>]
type OnReminderAttribute() = inherit Attribute()

// F#-specific grain subclass with a few of the methods made public.
type FSharpGrain() =
    inherit Grain()

    member internal me.registerTimer (f: 'a -> Task) (state: 'a) dueTime period =
        let handler (o: obj) = let a = o :?> 'a in f a
        me.RegisterTimer(Func<_,_>(handler), box state, dueTime, period)

type IGrainIdentity = interface end
type private IGrainIdentityInternal = abstract grain : FSharpGrain with get

// A wrapper around IGrain. Passing a clear IGrain into grain functions will
// give people the wrong idea about what they can do with it.
// We'll have different identity types for grains with different key types.
type GrainIdentityI<'IGrain when 'IGrain :> IGrainWithIntegerKey> =
    private GrainIdentity of 'IGrain * FSharpGrain
    with
        static member create<'TGrain when 'TGrain :> FSharpGrain> (grain: 'TGrain) = (grain.AsReference<'IGrain>(), grain :> FSharpGrain) |> GrainIdentity
        member me.key = let (GrainIdentity (ref, _)) = me in ref.GetPrimaryKeyLong()

        interface IGrainIdentity
        interface IGrainIdentityInternal with
            member me.grain = let (GrainIdentity (_, grain)) = me in grain

module Grain =
    let registerTimer (i: IGrainIdentity) f state dueTime period =
        (i |> box |> unbox<IGrainIdentityInternal>).grain.registerTimer f state dueTime period

// Mandatory input to all grain functions, also with different types
// for different grain key types.
type GrainFunctionInputI<'Services, 'IGrain when 'IGrain :> IGrainWithIntegerKey> = { 
    IdentityI: GrainIdentityI<'IGrain>
    Services: 'Services
    GrainFactory: IGrainFactory
    }

// Cache of FSharpFuncs. The cache is initialized by generated code at startup.
module __GrainFunctionCache =
    let internal methodCacheI = Dictionary<Type, (IGrainFactory * int64 -> obj)>()

    [<Obsolete("For internal use only; do not use directly.")>]
    let __registeri<'Grain when 'Grain :> IGrainWithIntegerKey>
        (t: Type, mkGrain: IGrainFactory * int64 -> 'Grain, mkFunc: 'Grain -> obj) =
            methodCacheI.Add(t, (fun (f: IGrainFactory, k: int64) -> mkGrain (f, k) |> mkFunc))

    let internal getProxyMethod (f: FSharpFunc<_,_>) =
        let t = f.GetType()
        match methodCacheI.TryGetValue t with
        | true, x -> x
        | false, _ ->  failwithf "Unknown function %s" t.FullName

// Implementation of proxies. Proxies are responsible for collecting
// all arguments and feeding them into interface functions at once.
module __ProxyFunctions =
    type call1<'p1, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, 'res>()
        override __.Invoke(x: 'p1) =
            let args = (x :> obj) :: args |> List.toArray
            Array.Reverse(args)
            method.Invoke(grainRef, args) :?> 'res

    type call2<'p1, 'p2, 'res>(grainRef: IGrain, method: MethodInfo, args: obj list) =
        inherit FSharpFunc<'p1, FSharpFunc<'p2, 'res>>()
        override __.Invoke(x: 'p1) = call1<'p2, 'res>(grainRef, method, x :> obj :: args) |> box |> unbox<FSharpFunc<'p2, 'res>>

type IGrainFactory with
    // Proxy generator. Operates in a completely type-safe manner. Takes an FSharpFunc
    // and builds a proxy with the same argument types.
    // TODO support partially applied grain functions as well
    member me.invokei (f: GrainFunctionInputI<_,_> -> 'tres) (key: int64) : 'tres =
        __GrainFunctionCache.getProxyMethod f <| (me, key) :?> 'tres
