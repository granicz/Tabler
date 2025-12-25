namespace Tabler

open System.IO
open WebSharper

type TableSourceGenerator() =
    interface ISourceGenerator with
        member this.Generate (gen: GenerateCall) : string array option =
            let file = gen.FilePath
            IDE.debug gen <| sprintf "Processing %s...\n" file
            File.ReadAllLines(file)
            |> Reader.ParseAll
            |> function
                | Ok res ->
                    // TODO: Sanity check to make sure all CVS rows contain enough columns
                    // Sanity check: the quantified name of the data source needs to be X.Y.Z
                    let qn = res.Header.QuantifiedName
                    if qn.Split([|'.'|]) |> Array.length <> 3 then
                        IDE.error gen (file, (1, 0), (1, qn.Length)) "Quantified name needs to be of the format X.Y.Z"
                        None
                    else
                        CodeGen.Generate res
                        |> List.map (fun cf ->
                            // Gets the parent folder of the original .table file
                            let outputPath = Path.GetDirectoryName(file)
                            let outputFile = Path.Combine(outputPath, file+"_"+cf.BaseFilename) |> Path.GetFullPath
                            let outputPath = Path.GetDirectoryName(outputFile)
                            if not (Directory.Exists(outputPath)) then
                                Directory.CreateDirectory(outputPath) |> ignore
                            // Write generated file
                            File.WriteAllText(outputFile, cf.Content)
                            outputFile
                        )
                        |> List.toArray
                        |> Some
                | Error e ->
                    let pos = (file, (e.StartLine, e.StartColumn), (e.StartLine, e.StartColumn+e.Length))
                    IDE.error gen pos e.Message
                    None

[<assembly:FSharpSourceGenerator("table", typeof<TableSourceGenerator>)>]
do ()

