namespace TennisBot

open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe

module Api =
    open Telegram

    let webHookRoute telegramToken = $"/api/{telegramToken}"

    let updateHandler (logger : ILogger) telegramToken : HttpHandler =
        bindModel<Update> None (fun update ->
            let name = update.Message.Value.From.Value.FirstName
            let chatId = update.Message.Value.Chat.Id
            sendMessage logger telegramToken chatId $"Hello {name}" |> Async.Start
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
