namespace MyApp

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home

module Site =
    let HomePage ctx =
        Content.Page(
            MainTemplate()
                .Content(Employees.Data)
                .Doc()
        )

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home ->
                HomePage ctx
        )

