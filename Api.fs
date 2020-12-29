namespace TennisBot

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

    let updateHandler (logger : ILogger) telegramToken : HttpHandler =
        bindModel<Update> None (fun update ->
            let name = update.Message.Value.From.Value.FirstName
            let chatId = update.Message.Value.Chat.Id
            scrapeAvailableCourts()
            |> Seq.map (fun availableCourt ->
                $"Court: {availableCourt.Court}, Time: {availableCourt.Time}")
            |> String.concat "\n"
            |> sendMessage logger telegramToken chatId
            |> ignore
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
