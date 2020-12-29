namespace TennisBot

open FSharp.Data

module BookingScraper =

    let meilahtiUrl = "https://meilahti.slsystems.fi/booking/booking-calendar"

    let parseAvailableTimes (doc : HtmlDocument) =
        doc.CssSelect(".s-avail > a")
        |> Seq.map (fun htmlNode ->
            let text = htmlNode.InnerText()
            let parts = text.Split(' ') 
            {| Court = parts.[0]; Time = parts.[1] |})

    let scrapeAvailableCourts () =
        let doc = HtmlDocument.Load(meilahtiUrl)
        parseAvailableTimes doc
