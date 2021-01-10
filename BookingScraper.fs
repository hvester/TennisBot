namespace TennisBot

open System
open System.Text.RegularExpressions
open FSharp.Data

module BookingScraper =

    type AvailableCourtTimeSlot =
        {
            TennisCenter : string
            Time : DateTime
            Court : string
            BookingLink : string
            BookingTableLink : string
        }

    let meilahtiBaseUrl = "https://meilahti.slsystems.fi"
    let meilahtiTennisTab = 1
    
    let taliTaivallahtiBaseUrl = "https://varaukset.talintenniskeskus.fi"
    let taliTennisTab = 1
    let taivallahtiTennisTab = 5    

    let tennisCenters =
        [
            ("Meilahti", meilahtiBaseUrl, 1)
            ("Taivallahti", taliTaivallahtiBaseUrl, 5)
            ("Tali", taliTaivallahtiBaseUrl, 1)
        ]

    
    let generateBookingPageUrl baseUrl tabNumber (date : DateTime) =
        [
            $"{baseUrl}/booking/booking-calendar"
            $"?BookingCalForm%%5Bp_laji%%5D={tabNumber}"
            $"""&BookingCalForm%%5Bp_pvm%%5D={date.ToString("yyyy-MM-dd")}"""
        ]
        |> String.concat ""


    let parseDate (doc : HtmlDocument) =
        let htmlNode = doc.CssSelect("#bookingcalform-p_pvm_custom") |> Seq.head
        let dateStr = htmlNode.Attribute("value").Value()
        let regexGroups = Regex("(\d+).(\d+).(\d+)").Match(dateStr).Groups
        let day = Int32.Parse regexGroups.[1].Value
        let month = Int32.Parse regexGroups.[2].Value
        let year = Int32.Parse regexGroups.[3].Value
        (year, month, day)


    let parseAvailableTimeSlots (doc : HtmlDocument) =
        doc.CssSelect(".s-avail > a")
        |> Seq.map (fun htmlNode ->
            let text = htmlNode.InnerText()
            let parts = text.Split([|' '; ':'; '\n'|])
            let court = parts.[0]
            let hours = Int32.Parse parts.[1]
            let minutes = Int32.Parse parts.[2]
            let relativeLink = htmlNode.Attribute("href").Value()
            (hours, minutes, court, relativeLink))


    let scrapeBookingPage tennisCenterName baseUrl tabNumber date =
        async {
            let bookingTableUrl = generateBookingPageUrl baseUrl tabNumber date
            let! doc = HtmlDocument.AsyncLoad(bookingTableUrl)
            // If booking table is not available for the requested date then default
            // booking page is returned (which is same or next day)
            let year, month, day = parseDate doc
            if year = date.Year && month = date.Month && day = date.Day then
                return
                    parseAvailableTimeSlots doc
                    |> Seq.map (fun (hours, minutes, court, relativeLink) ->
                        {
                            TennisCenter = tennisCenterName
                            Time = DateTime(year, month, day, hours, minutes, 0)
                            Court = court
                            BookingLink = baseUrl + relativeLink
                            BookingTableLink = bookingTableUrl
                        })
                    |> Seq.toList
            else
                // Redirected to other page, i.e. no courts available for the requested date
                return []
        }


    let scrapeAvailableCourts () =
        async {
            let today = DateTime.Now.Date
            let! allAvailableCourts =
                [
                    for n in 0 .. 7 do
                        let date = today.AddDays(float n)
                        for tennisCenterName, baseUrl, tabnumber in tennisCenters do
                            yield scrapeBookingPage tennisCenterName baseUrl tabnumber date ]
                |> Async.Parallel
            return
                allAvailableCourts
                |> Array.toList
                |> List.concat
        }
