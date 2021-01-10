namespace TennisBot

open System
open Microsoft.FSharp.Reflection

module TennisBot =
    open BookingScraper

    type Command =
        | ShowBestCourts
        | ShowAllCourts

    type Message =
        | Command of Command
        | Text of string

    let maxTelegramMessageLength = 4096

    let toMessages maxMessageSize (parts : string list) =
        [
            let mutable messageParts = ResizeArray()
            let mutable messageSize = 0
            for part in parts do
                let newMessageSize = messageSize + part.Length
                if messageParts.Count = 0 || newMessageSize <= maxMessageSize then
                    messageParts.Add part
                    messageSize <- newMessageSize
                else
                    yield messageParts
                    messageParts <- ResizeArray([part])
                    messageSize <- part.Length

            if messageParts.Count > 0 then
                yield messageParts
        ]
        |> List.map (String.concat "\n")


    let renderAvailableCourts availableCourts =
        availableCourts
        |> List.groupBy (fun x -> (x.TennisCenter, x.Time.Date))
        |> List.map (fun ((tennisCenter, date), courts) ->
            [
                $"""<b><u>{tennisCenter} - {date.DayOfWeek} {date.ToString("d.M.")}</u></b>"""
                for time, group in courts |> Seq.groupBy (fun c -> c.Time) do
                    let courtCodes =
                        group
                        |> Seq.map (fun c ->
                            $"""<a href="{c.BookingLink}">{c.Court}</a>""")
                    $"""<b>{time.ToString("HH:mm")}:</b> {String.concat ", " courtCodes}"""
                ""
            ]
            |> String.concat "\n")
        |> toMessages maxTelegramMessageLength


    let renderAvailableCourtsCompactly availableCourts =
        availableCourts
        |> List.groupBy (fun x -> x.Time.Date)
        |> List.map (fun (date, courtsForDate) ->
            [
                $"""<b><u>{date.DayOfWeek} {date.ToString("d.M.")}</u></b>"""

                for tennisCenterName, courts in List.groupBy (fun x -> x.TennisCenter) courtsForDate do
                    [
                        let bookingTableLink = (List.head courts).BookingTableLink
                        $"""<a href="{bookingTableLink}"><b>{tennisCenterName}</b></a>: """

                        courts
                        |> List.groupBy (fun c -> c.Time)
                        |> List.map (fun (time, group) ->
                            let firstBookingLink = (List.head group).BookingLink
                            $"""<a href="{firstBookingLink}">{time.ToString("HH:mm")}</a>""")
                        |> String.concat ", "
                    ]
                    |> String.concat ""

                ""
            ]
            |> String.concat "\n")
        |> toMessages maxTelegramMessageLength

    
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


    let filterBestAvailableCourts (availableCourts : AvailableCourtTimeSlot list) =
        let now = DateTime.UtcNow.AddHours(2.0) // TODO: Fix this hack with NodaTime
        availableCourts
        |> List.distinctBy (fun x -> x.Time)
        |> List.sortByDescending (fun x ->
            let hour = x.Time.Hour
            let timePoints =
                if hour < 15 then 0
                elif hour < 16 then 1
                elif hour < 18 then 3
                elif hour < 20 then 5
                elif hour < 21 then 3
                elif hour < 22 then 1
                else 0
            let dayPoints =
                8.0 - x.Time.Subtract(now).TotalDays
                |> min 5.0
                |> max 0.0
            float (1 + timePoints) * (1.0 + dayPoints))
        |> List.truncate 5
                

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
            |> List.singleton
            |> async.Return

        | Command ShowBestCourts ->
            async {
                let! availableCourts = scrapeAvailableCourts()
                return renderAvailableCourts (filterBestAvailableCourts availableCourts)
            }


        | Command ShowAllCourts ->
            async {
                let! availableCourts = scrapeAvailableCourts()
                return renderAvailableCourtsCompactly availableCourts
            }
