namespace Tabler

module Reader =
    open System.Text.RegularExpressions

    type ParseError =
        {
            Message: string
            StartLine: int
            StartColumn: int
            Length: int
        }

    type Cursor =
        {
            Text: string
            LineNumber: int
            Column: int
        }

    type FieldType =
        | String
        | Int
        | Union
        | Date

    type DataHeader =
        {
            QuantifiedName: string
            TypeName: string
        }

    type DataColumn =
        {
            ColumnName: string
            FieldName: string
            FieldType: FieldType
        }

    type ParsedData =
        {
            Header: DataHeader
            Columns: DataColumn list
            CsvHead: string list
            CsvRows: string list list
        }

    type ResultBuilder() =
        member _.Bind(x, f) =
            match x with
            | Ok v    -> f v
            | Error e -> Error e

        member _.Return(x) = Ok x
        member _.ReturnFrom(x) = x

    let result = ResultBuilder()

    let error msg (line, col, length) =
        Error { Message = msg; StartLine = line; StartColumn = col; Length = length }

    let tryMatch (lineNo, rxName, rx: Regex) (c: Cursor) =
        let m = rx.Match(c.Text, c.Column)
        if m.Success && m.Index = c.Column then
            Ok (m, { c with Column = c.Column + m.Length })
        else
            error $"Expected {rxName}" (lineNo, c.Column, 1)

    let ParseHeaderLine (line: string, lineNo) =
        let c0 = { Text = line; LineNumber = lineNo; Column = 0 }
        // # MyApp.Employees = Person list {
        result {
            let! (_, c1) = tryMatch (lineNo, "whitespace", Regex @"\s*#\s*") c0
            let! (qn, c2) = tryMatch (lineNo, "target identifier", Regex @"(?<q>(?:[A-Za-z_][A-Za-z0-9_]*\.)*[A-Za-z_][A-Za-z0-9_]*)") c1
            let! (_, c3) = tryMatch (lineNo, "=", Regex @"\s*=\s*") c2
            let! (item, c4) = tryMatch (lineNo, "item identifier", Regex @"[A-Za-z_][A-Za-z0-9_]*") c3
            let! (_, c5) = tryMatch (lineNo, "'list'", Regex @"\s+list\s+") c4
            let! (_, _)  = tryMatch (lineNo, "'{'", Regex @"\{\s*$") c5
            return
                {
                    DataHeader.QuantifiedName = qn.Groups["q"].Value
                    DataHeader.TypeName = item.Value
                }
        }

    let ParseMappingLine (line: string, lineNo) =
        let c0 = { Text = line; LineNumber = lineNo; Column = 0 }
        // #    "First Name" -> FirstName: string
        result {
            let! (_, c1) = tryMatch (lineNo, "whitespace", Regex @"\s*#\s*") c0
            let! (q, c2) = tryMatch (lineNo, "column name", Regex "\"(?<col>[^\"]*)\"") c1
            let! (_, c3) = tryMatch (lineNo, "->", Regex @"\s*->\s*") c2
            let! (f, c4) = tryMatch (lineNo, "field name", Regex @"[A-Za-z_][A-Za-z0-9_]*") c3
            let! (_, c5) = tryMatch (lineNo, ":", Regex @"\s*:\s*") c4
            let! (t, _)  = tryMatch (lineNo, "field type", Regex @"[A-Za-z_][A-Za-z0-9_]*") c5
            let! tt =
                match t.Value.ToLower() with
                | "string" ->
                    Ok FieldType.String
                | "int" ->
                    Ok FieldType.Int
                | "union" ->
                    Ok FieldType.Union
                | "date" ->
                    Ok FieldType.Date
                | _ ->
                    error $"Valid field type expected, got {t.Value}" (lineNo, c5.Column, t.Value.Length) 
            return
                {
                    DataColumn.ColumnName = q.Groups["col"].Value
                    DataColumn.FieldName = f.Value
                    DataColumn.FieldType = tt
                }
        }

    let ParseCloserLine (line: string, lineNo) =
        let c0 = { Text = line; LineNumber = lineNo; Column = 0 }
        // # }
        result {
            let! (_, c1) = tryMatch (lineNo, "whitespace", Regex @"\s*#\s*") c0
            let! (_, c2) = tryMatch (lineNo, "}", Regex @"}\s*$") c1
            return true
        }

    let splitCsvLine (line: string) =
        line.Split(',')
        |> Array.map (fun s -> s.Trim())
        |> Array.toList

    let ParseSchemaBlock (lines: (int * string) list) =
        result {
            match lines with
            | [] ->
                return! error "Expected header line" (0, 0, 0)
            | (ln, line) :: rest ->
                let! header = ParseHeaderLine (line, ln)
                let rec parseMappings acc remaining =
                    result {
                        match remaining with
                        | [] ->
                            return! error "Unexpected end of file in schema block" (ln, 0, 0)
                        | (ln, line) :: tail ->
                            match ParseCloserLine (line, ln) with
                            | Ok _ ->
                                return List.rev acc, tail
                            | Error _ ->
                                let! col = ParseMappingLine (line, ln)
                                return! parseMappings (col :: acc) tail
                    }
                let! columns, restLines = parseMappings [] rest
                return header, columns, restLines
        }

    let ParseCsv (lines: (int * string) list) =
        result {
            match lines with
            | [] ->
                return! error "Expected CSV header" (0, 0, 0)
            | (_, headerLine) :: rows ->
                let csvHeader = splitCsvLine headerLine
                let csvRows =
                    rows
                    |> List.map (fun (_, l) -> splitCsvLine l)
                return csvHeader, csvRows
        }

    let ParseAll (lines: string array) =
        let lines =
            lines
            |> Array.mapi (fun i l -> i + 1, l)
            |> Array.toList
            |> List.filter (fun (_, l) -> not (System.String.IsNullOrWhiteSpace l))
        result {
            let! header, columns, rest = ParseSchemaBlock lines
            let! csvHeader, csvRows = ParseCsv rest
            return
                {
                    Header  = header
                    Columns = columns
                    CsvHead = csvHeader
                    CsvRows = csvRows
                }
        }
