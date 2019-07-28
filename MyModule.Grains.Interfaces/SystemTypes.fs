module SystemTypes

open System.Threading.Tasks

// Special methods used when doing calls to other grains from within the silo
type IFSharpGrain =
    abstract InvokeFunc: obj -> Task
    abstract InvokeFuncWithResult: obj -> Task<obj>