module HelloArgs

(* 
   Any types that need to be in the public interface of the
   module go in a separate DLL which I'll call "Common". This
   is because we'll codegen the entire grain interface assembly,
   so users can't really place any other code in there.
*)

type T = {
    Name: string
    }

let create name = { Name = name }

let getName { Name = name} = name