namespace Tabler

open PP
open Reader
open Helpers

module CodeGen =
    type UnionValues =
        {
            Column: DataColumn
            Values: string list
        }

    let ExtractUnionValues (data: ParsedData) : UnionValues list =
        let indexByName =
            data.CsvHead
            |> List.mapi (fun i name -> name, i)
            |> Map.ofList
        data.Columns
        |> List.choose (fun col ->
            match col.FieldType with
            | FieldType.Union ->
                match Map.tryFind col.ColumnName indexByName with
                | None ->
                    None
                | Some idx ->
                    let values =
                        data.CsvRows
                        |> List.choose (fun row ->
                            if idx < row.Length then
                                let v = row[idx].Trim()
                                if v = "" then None else Some v
                            else
                                None)
                        |> Set.ofList
                        |> Set.toList
                        |> List.sort
                    Some
                        { Column = col
                          Values = values }
            | _ ->
                None)

    let Generate (data: Reader.ParsedData) = [
        let cfb = IndentedCodeFileBuilder("Data.fs")
        let pp = cfb.Builder

        let nss = data.Header.QuantifiedName.Split([|'.'|])
        let ns, m, v = nss.[0], nss.[1], nss.[2]
        pp.NAMESPACE ns
        pp.EMPTY_LINE()
        pp.OPEN "WebSharper.UI.Templating"
        pp.EMPTY_LINE()
        pp.AddLine 0 $"type MainTemplate=Template<\"Main.html\", ClientLoad.FromDocument>"
        pp.EMPTY_LINE()
        pp.MODULE false (capitalize m)
        pp.OPEN "System"
        pp.EMPTY_LINE()
        // Print types for each union
        ExtractUnionValues data
        |> List.iter (fun union ->
            pp.AddLine 0 $"type {union.Column.FieldName} = "
            union.Values
            |> List.iter (fun uv ->
                pp.AddLine 1 $"| {toPascalCase uv}"
            )
            pp.EMPTY_LINE()
            pp.AddLine 1 $"override this.ToString() ="
            pp.AddLine 2 $"match this with"
            union.Values
            |> List.iter (fun uv ->
                pp.AddLine 3 $"| {toPascalCase uv} -> \"{uv}\""
            )
            pp.EMPTY_LINE()
        )
        // Print main type
        pp.AddLine 0 $"type {capitalize data.Header.TypeName} ="
        pp.AddLine 1 "{"
        data.Columns
        |> List.iter (fun col ->
            let colTy =
                match col.FieldType with
                | FieldType.String ->
                    "string"
                | FieldType.Int ->
                    "int"
                | FieldType.Union ->
                    capitalize col.FieldName
                | FieldType.Date ->
                    "DateTime"
            pp.AddLine 2 $"{capitalize col.FieldName}: {colTy}"
        )
        pp.AddLine 1 "}"
        pp.EMPTY_LINE()
        // Create member
        let pars =
            data.Columns
            |> List.map (fun c -> lowerFirst c.FieldName)
            |> String.concat ", "
        pp.AddLine 1 $"static member Create ({pars}) ="
        pp.AddLine 2 "{"
        data.Columns
        |> List.iter (fun c ->
            pp.AddLine 3 $"{capitalize c.FieldName} = {lowerFirst c.FieldName}"
        )
        pp.AddLine 2 "}"
        pp.EMPTY_LINE()
        //
        pp.AddLine 0 $"let headerColumn (col: string) ="
        pp.AddLine 1 $"MainTemplate.{capitalize m}Table_HeaderColumn()"
        pp.AddLine 2 $".Header(col)"
        pp.AddLine 2 $".Doc()"
        pp.EMPTY_LINE()
        //
        pp.AddLine 0 $"let row ({lowerFirst data.Header.TypeName}:{capitalize data.Header.TypeName}) ="
        pp.AddLine 1 $"MainTemplate.{capitalize (singularize m)}()"
        data.Columns
        |> List.iter (fun c ->
            match c.FieldType with
            | FieldType.String ->
                pp.AddLine 2 $".{capitalize c.FieldName}({lowerFirst data.Header.TypeName}.{capitalize c.FieldName})"
            | FieldType.Int ->
                pp.AddLine 2 $".{capitalize c.FieldName}(string {lowerFirst data.Header.TypeName}.{capitalize c.FieldName})"
            | FieldType.Union ->
                pp.AddLine 2 $".{capitalize c.FieldName}({lowerFirst data.Header.TypeName}.{capitalize c.FieldName}.ToString())"
            | FieldType.Date ->
                pp.AddLine 2 $".{capitalize c.FieldName}({lowerFirst data.Header.TypeName}.{capitalize c.FieldName}.ToShortDateString())"
        )
        pp.AddLine 2 $".Doc()"
        pp.EMPTY_LINE()
        pp.AddLine 0 $"let {capitalize v} ="
        pp.AddLine 1 $"MainTemplate.{capitalize m}()"
        pp.AddLine 2 $".HeaderRow(["
        data.Columns
        |> List.iter (fun c ->
            pp.AddLine 3 $"headerColumn \"{c.ColumnName}\""
        )
        pp.AddLine 2 $"])"
        pp.AddLine 2 $".Data(["
        data.CsvRows
        |> List.iter (fun row ->
            let vs =
                List.zip data.Columns row
                |> List.map (fun (c, row) ->
                    match c.FieldType with
                    | FieldType.String ->
                        $"\"{row}\""
                    | FieldType.Int ->
                        $"{row}"
                    | FieldType.Union ->
                        $"{capitalize c.FieldName}.{toPascalCase row}"
                    | FieldType.Date ->
                        $"DateTime.Parse \"{row}\""
                )
                |> String.concat ", "
            pp.AddLine 3 $"row <| {capitalize data.Header.TypeName}.Create ({vs})"
        )
        pp.AddLine 2 "])"
        pp.AddLine 2 $".Count(string {data.CsvRows.Length})"
        pp.AddLine 2 ".Doc()"
        pp.EMPTY_LINE()
        pp.AddLine 0 $"let DataCount = {data.CsvRows.Length}"
        yield cfb.OutputCodeFileOf()
    ]
