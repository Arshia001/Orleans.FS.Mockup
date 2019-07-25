module SystemTypes

open System
open Orleans

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
type GrainIdentity =
    private GrainIdentity of IGrain
    with
        static member create (ref: IGrain) = ref.AsReference<IGrain>() |> GrainIdentity
        member me.longKey () = let (GrainIdentity ref) = me in ref.GetPrimaryKeyLong()

// Mandatory input to all grain functions.
type GrainFunctionInput<'TServices> = { 
    Identity: GrainIdentity
    Services: 'TServices
    }

// TODO: How do we reference other grains *without* access to the interfaces library?
let getHelloWorker id = Unchecked.defaultof<HelloWorkerGrain.IHelloWorkerGrain>