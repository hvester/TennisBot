namespace TennisBot

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open System.Threading.Tasks

module Api =
    open Telegram
    open BookingScraper

    let webHookRoute telegramToken = $"/api/{telegramToken}"

    let renderAvailableCourts (availableCourts : (DateTime * seq<{| Court: string; Time: string |}>) array) =
        [
            for date, courts in availableCourts do
                $"""---Date: {date.ToString("MM-dd")}---"""
                for time, group in courts |> Seq.groupBy (fun c -> c.Time) do
                    let courtCodes = group |> Seq.map (fun c -> c.Court)
                    $"""Time: {time}, Courts: {String.concat ", " courtCodes}"""
        ]
        |> String.concat "\n"

    let updateHandler (logger : ILogger) telegramToken : HttpHandler =
        bindModel<Update> None (fun update ->
            let chatId = update.Message.Value.Chat.Id
            async {
                let! availableCourts = scrapeAvailableCourts()
                renderAvailableCourts availableCourts
                |> sendMessage logger telegramToken chatId
                |> ignore
            }
            |> Async.Start
            Successful.OK "")

    let logRequest (logger : ILogger) : HttpHandler =
        handleContext (fun ctx ->
            task {
                ctx.Request.EnableBuffering()
                use reader = new StreamReader(ctx.Request.Body, leaveOpen=true)
                let! body = reader.ReadToEndAsync()
                ctx.Request.Body.Position <- 0L
                logger.LogInformation($"Method: {ctx.Request.Method}\nBody: {body}")
                return Some ctx
            })

    let app logger telegramToken : HttpHandler =
        logRequest logger >=>
            choose [
                POST >=> route (webHookRoute telegramToken) >=> updateHandler logger telegramToken
            ]
