namespace TennisBot

open System
open System.Net
open FSharp.Data

module BookingScraper =

    
    let meilahtiUrl = "https://meilahti.slsystems.fi/booking/booking-calendar"

    
    let generateBookingPageUrl baseUrl (date : DateTime) =
        $"""{baseUrl}?BookingCalForm%%5Bp_pvm%%5D={date.ToString("yyyy-MM-dd")}"""


    let parseAvailableTimes (doc : HtmlDocument) =
        doc.CssSelect(".s-avail > a")
        |> Seq.map (fun htmlNode ->
            let text = htmlNode.InnerText()
            let parts = text.Split([|' '; '\n'|])
            {| Court = parts.[0]; Time = parts.[1] |})


    let scrapeBookingPage date =
        async {
            let url = generateBookingPageUrl meilahtiUrl date
            let! doc = HtmlDocument.AsyncLoad(url)
            return (date, parseAvailableTimes doc)
        }


    let scrapeAvailableCourts () =
        let today = DateTime.Now.Date
        [ for n in 0 .. 7 do
            let date = today.AddDays(float n)
            yield scrapeBookingPage date ]
        |> Async.Parallel
