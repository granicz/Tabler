namespace Tabler

open System.IO
open WebSharper

type filePos = string * (int * int) * (int * int)

type OutputCodeFile =
    {
        BaseFilename: string
        Content: string
    }

module Helpers =
    let capitalize (s: string) =
        if System.String.IsNullOrEmpty s then
            s
        else
            s.Substring(0, 1).ToUpperInvariant() + s.Substring(1)

    let lowerFirst (s: string) =
        if System.String.IsNullOrEmpty s then
            s
        else
            s.Substring(0, 1).ToLowerInvariant() + s.Substring(1)

    let singularize (s: string) =
        if System.String.IsNullOrEmpty s then
            s
        else
            if s.EndsWith("ies", System.StringComparison.OrdinalIgnoreCase) then
                s.Substring(0, s.Length - 3) + "y"
            elif s.EndsWith("s", System.StringComparison.OrdinalIgnoreCase) then
                s.Substring(0, s.Length - 1)
            else
                s
    
    let toPascalCase (s: string) =
        if System.String.IsNullOrWhiteSpace s then
            ""
        else
            s.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun part ->
                let p = part.Trim()
                if p = "" then ""
                else p.[0..0].ToUpperInvariant() + p.[1..])
            |> String.concat ""

module IDE =
    open System

    let error gen ((filename, (_, _), (_, _)) as pos) msg =
        let ppos (_, (startLine, startColumn), (endLine, endColumn)) =
            $"{startLine},{startColumn+1},{endLine},{endColumn+1}"
        gen.PrintError <| $"{filename}({ppos(pos)}): error WS9011: {msg}."
        
    let debug (gen: GenerateCall) msg =
        let dir = gen.ProjectFilePath |> Path.GetDirectoryName
        let fpath = Path.Combine(dir, "tabler.log")
        File.AppendAllText(fpath, sprintf "%A %s\n" (DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")) msg)


module PP =
    open System
    open System.Text

    type IndentedStringBuilder(s: string) =
        let sb = new StringBuilder(s)
    
        new () = IndentedStringBuilder("")

        member val TAB_SIZE = 4 with get, set
        member val CURRENT_INDENT = 0 with get, set

        member this.AddIndent () = this.CURRENT_INDENT <- this.CURRENT_INDENT + 1

        member this.RemoveIndent () =
            if this.CURRENT_INDENT > 0 then
                this.CURRENT_INDENT <- this.CURRENT_INDENT - 1
            else
                this.CURRENT_INDENT <- 0

        member this.AddLine ind s =
            let tab = String.init this.TAB_SIZE (fun _ -> " ")
            let indent = String.init (this.CURRENT_INDENT+ind) (fun _ -> tab)
            sb.AppendLine (sprintf "%s%s" indent s) |> ignore

        member __.AddString ind (s: string) =
            for line in s.Split([| System.Environment.NewLine; "\r"; "\n"|], StringSplitOptions.None) do
                __.AddLine ind line

        member __.StringOf() =
            sb.ToString()

        member pp.EMPTY_LINE () =
            pp.AddLine 0 ""

        member pp.NAMESPACE ns =
            pp.AddLine 0 <| sprintf "namespace %s" ns

        member pp.OPEN ns =
            pp.AddLine 0 <| sprintf "open %s" ns

        member pp.OPEN_TYPE ns =
            pp.AddLine 0 <| sprintf "open type %s" ns

        member pp.ATTRIBUTE att =
            pp.AddLine 0 <| sprintf "[<%s>]" att

        member pp.MODULE recu mo =
            if recu then
                pp.AddLine 0 <| sprintf "and %s =" mo
            else
                pp.AddLine 0 <| sprintf "module %s =" mo
            pp.AddIndent()

        member pp.MODULE_NOT_INDENTED mo =
            pp.AddLine 0 <| sprintf "module %s" mo

        member pp.MODULE_END () =
            pp.RemoveIndent()

        member pp.TYPE_RECORD recu ty fields =
            if recu then
                pp.AddLine 0 <| sprintf "and %s =" ty
            else
                pp.AddLine 0 <| sprintf "type %s =" ty
            pp.AddLine 1 "{"
            for (f, v) in fields do
                pp.AddLine 2 <| sprintf "%s: %s" f v
            pp.AddLine 1 "}"

    type IndentedCodeFileBuilder(fname, content) =
        let isb = IndentedStringBuilder(content)

        new (fname) = IndentedCodeFileBuilder(fname, "")

        member this.Builder = isb

        member this.OutputCodeFileOf() =
            {
                BaseFilename = fname
                Content = isb.StringOf()
            }
