namespace TennisBot

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open System.Threading.Tasks

module Api =
    open TennisBot
    open Telegram
    open BookingScraper

    let webHookRoute telegramToken = $"/api/{telegramToken}"


    let updateHandler (logger : ILogger) telegramToken : HttpHandler =
        bindModel<Dto.Update> None (fun update ->
            let chatId = update.Message.Value.Chat.Id
            update.Message
            |> Option.map Dto.toDomainMessage
            |> function
                | Some (Ok message) ->
                    async {
                        let! respose = handleMessage message
                        do! sendMessage logger telegramToken chatId respose |> Async.AwaitTask
                    }
                    |> Async.Start

                | Some (Error errorMessage) ->
                    sendMessage logger telegramToken chatId errorMessage |> ignore
                
                | None ->
                    ()
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
