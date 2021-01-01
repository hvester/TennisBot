namespace TennisBot

open System
open System.Text.RegularExpressions
open FSharp.Data

module BookingScraper =

    type AvailableCourtTimeSlot =
        { Time : DateTime
          Court : string }
    
    let meilahtiUrl = "https://meilahti.slsystems.fi/booking/booking-calendar"

    
    let generateBookingPageUrl baseUrl (date : DateTime) =
        $"""{baseUrl}?BookingCalForm%%5Bp_pvm%%5D={date.ToString("yyyy-MM-dd")}"""


    let parseDate (doc : HtmlDocument) =
        let htmlNode = doc.CssSelect("#bookingcalform-p_pvm_custom") |> Seq.head
        let dateStr = htmlNode.Attribute("value").Value()
        let regexGroups = Regex("(\d+).(\d+).(\d+)").Match(dateStr).Groups
        let day = Int32.Parse regexGroups.[1].Value
        let month = Int32.Parse regexGroups.[2].Value
        let year = Int32.Parse regexGroups.[3].Value
        (year, month, day)


    let parseAvailableTimes (doc : HtmlDocument) =
        let year, month, day = parseDate doc
        doc.CssSelect(".s-avail > a")
        |> Seq.map (fun htmlNode ->
            let text = htmlNode.InnerText()
            let parts = text.Split([|' '; ':'; '\n'|])
            let court = parts.[0]
            let hours = Int32.Parse parts.[1]
            let minutes = Int32.Parse parts.[2]
            { Time = DateTime(year, month, day, hours, minutes, 0); Court = court })


    let scrapeBookingPage date =
        async {
            let url = generateBookingPageUrl meilahtiUrl date
            let! doc = HtmlDocument.AsyncLoad(url)
            return parseAvailableTimes doc
        }


    let scrapeAvailableCourts () =
        async {
            let today = DateTime.Now.Date
            let! allAvailableCourts =
                [ for n in 0 .. 7 do
                    let date = today.AddDays(float n)
                    yield scrapeBookingPage date ]
                |> Async.Parallel
            return Seq.concat allAvailableCourts |> Seq.distinct |> Seq.toList
        }
