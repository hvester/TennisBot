namespace TennisBot

open System.Text
open System.Net.Http
open Microsoft.Extensions.Logging

module Telegram =

    [<CLIMutable>]
    type User =
        {
            Id : int64
            FirstName : string
            LastName : string
        }

    [<CLIMutable>]
    type Chat =
        {
            Id : int64
        }

    [<CLIMutable>]
    type Message =
        {
            MessageId : int64
            Text : string option
            From : User option
            Chat : Chat
        }

    [<CLIMutable>]
    type Update =
        {
            UpdateId : int
            Message : Message option
        }

    let httpClient = new HttpClient()

    let getUrl telegramToken method = $"https://api.telegram.org/bot{telegramToken}/{method}"

    let sendMessage (logger : ILogger) telegramToken chatId text =
        let url = getUrl telegramToken "sendMessage"
        let jsonString =
            {|
                ChatId = string chatId
                Text = text
            |}
            |> Json.serialize
        let content = new StringContent(jsonString, Encoding.UTF8, "application/json")
        async {
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            logger.LogInformation responseString
        }
