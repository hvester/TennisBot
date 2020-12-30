namespace TennisBot

open System
open Microsoft.FSharp.Reflection

module TennisBot =
    open BookingScraper

    type Command =
        | ShowAllCourts

    type Message =
        | Command of Command
        | Text of string


    let renderAvailableCourts (availableCourts : (DateTime * seq<{| Court: string; Time: string |}>) array) =
        [
            for date, courts in availableCourts do
                $"""---Date: {date.ToString("MM-dd")}---"""
                for time, group in courts |> Seq.groupBy (fun c -> c.Time) do
                    let courtCodes = group |> Seq.map (fun c -> c.Court)
                    $"""Time: {time}, Courts: {String.concat ", " courtCodes}"""
        ]
        |> String.concat "\n"

    
    let toSnakeCase (str : string) =
        if String.IsNullOrEmpty str then
            str
        else
            let withUnderScores =
                str.Substring(1)
                |> String.collect (fun c -> if Char.IsUpper c then $"_{c}" else string c)
                |> sprintf "%c%s" str.[0]
            withUnderScores.ToLowerInvariant()


    let allCommands =
        FSharpType.GetUnionCases(typeof<Command>)
        |> Array.map (fun unionCaseInfo ->
            FSharpValue.MakeUnion(unionCaseInfo, [||]) :?> Command)        


    let commandToString(x : Command) = 
        let case, _ = FSharpValue.GetUnionFields(x, typeof<Command>)
        $"/{toSnakeCase case.Name}"


    let handleMessage message =
        match message with
        | Text _ ->
            [
                "Hello!"
                "My name is Teppo TennisBot. How can I help you?"
                for command in allCommands do
                    commandToString command
            ]
            |> String.concat "\n"
            |> async.Return

        | Command ShowAllCourts ->
            async {
                let! availableCourts = scrapeAvailableCourts()
                return renderAvailableCourts availableCourts
            }
