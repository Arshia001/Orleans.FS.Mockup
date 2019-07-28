module SystemTypes

open System
open SystemTypes
open Orleans
open System.Threading.Tasks
open System.Collections.Immutable

// Everything in this file belongs inside Orleans.

// Similar to IPersistentState, only this one just holds a piece of data
// between calls, no persisting included.
[<Interface>]
type ITransientState<'t> =
    abstract Value: 't with get, set

type IGrainModuleWithIntegerKey = interface end
type IGrainModuleWithGuidKey = interface end

// A wrapper around IGrain. Passing a clear IGrain into grain functions will
// give people the wrong idea about what they can do with it.
type GrainIdentity =
    private GrainIdentity of IGrain
    with
        static member create (ref: IGrain) = ref.AsReference<IGrain>() |> GrainIdentity
        member me.longKey () = let (GrainIdentity ref) = me in ref.GetPrimaryKeyLong()

// Mandatory input to all grain functions.
type GrainFunctionInput<'TServices> = { 
    Identity: GrainIdentity
    Services: 'TServices
    GrainFactory: IGrainFactory
    }

// The code below will support calling other grains from within the silo
// TODO will grains within one assembly call grains from another assembly in the same way?

exception GrainTypeNotFound of Type

let mapTask (f: 'a -> 'b) (t: Task<'a>) = t.ContinueWith((fun (t: Task<'a>) -> f t.Result), TaskContinuationOptions.OnlyOnRanToCompletion)

// We'll have these for all grain key types, of course

let mutable longGrains: ImmutableDictionary<Type, IGrainFactory * int64 -> IFSharpGrain> = ImmutableDictionary<_,_>.Empty
let registerLong<'TGrain when 'TGrain :> IGrainWithIntegerKey and 'TGrain :> IFSharpGrain> moduleType =
    longGrains <- longGrains.Add(moduleType, fun (gf: IGrainFactory, key: int64) -> gf.GetGrain<'TGrain>(key) :> IFSharpGrain)    

let mutable guidGrains: ImmutableDictionary<Type, IGrainFactory * Guid -> IFSharpGrain> = ImmutableDictionary<_,_>.Empty
let registerGuid<'TGrain when 'TGrain :> IGrainWithGuidKey and 'TGrain :> IFSharpGrain> moduleType =
    guidGrains <- guidGrains.Add(moduleType, fun (gf: IGrainFactory, key: Guid) -> gf.GetGrain<'TGrain>(key) :> IFSharpGrain)

let invokeWithResult (grain: IFSharpGrain, f: 'TGrain -> Task<'TResult>) =
    let f i = f i |> mapTask (fun x -> x :> obj)
    grain.InvokeFuncWithResult f |> mapTask (fun x -> x :?> 'TResult)

let invokeWithoutResult (grain: IFSharpGrain, f: 'TGrain -> Task) = grain.InvokeFunc f

module Grain =
    let invokeir<'TGrain, 'TResult, 'TServices when 'TGrain :> IGrainModuleWithIntegerKey> (i: GrainFunctionInput<'TServices>) (key: int64) (f: 'TGrain -> Task<'TResult>) : Task<'TResult> =
        match longGrains.TryGetValue(typeof<'TGrain>) with
        | true, g -> invokeWithResult (g (i.GrainFactory, key), f)
        | false, _ -> GrainTypeNotFound typeof<'TGrain> |> raise

    let invokei<'TGrain, 'TServices when 'TGrain :> IGrainModuleWithIntegerKey> (i: GrainFunctionInput<'TServices>) (key: int64) (f: 'TGrain -> Task) : Task =
        match longGrains.TryGetValue(typeof<'TGrain>) with
        | true, g -> invokeWithoutResult (g (i.GrainFactory, key), f)
        | false, _ -> GrainTypeNotFound typeof<'TGrain> |> raise

    let invokegr<'TGrain, 'TResult, 'TServices when 'TGrain :> IGrainModuleWithGuidKey> (i: GrainFunctionInput<'TServices>) (key: Guid) (f: 'TGrain -> Task<'TResult>) : Task<'TResult> =
        match guidGrains.TryGetValue(typeof<'TGrain>) with
        | true, g -> invokeWithResult (g (i.GrainFactory, key), f)
        | false, _ -> GrainTypeNotFound typeof<'TGrain> |> raise

    let invokeg<'TGrain, 'TServices when 'TGrain :> IGrainModuleWithGuidKey> (i: GrainFunctionInput<'TServices>) (key: Guid) (f: 'TGrain -> Task) : Task =
        match guidGrains.TryGetValue(typeof<'TGrain>) with
        | true, g -> invokeWithoutResult (g (i.GrainFactory, key), f)
        | false, _ -> GrainTypeNotFound typeof<'TGrain> |> raise
