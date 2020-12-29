namespace TennisBot

open System.Text
open System.Net.Http
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive

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

    let private httpClient = new HttpClient()

    let private getUrl telegramToken method = $"https://api.telegram.org/bot{telegramToken}/{method}"

    let private makeRequest (logger : ILogger) telegramToken method payload = 
        let url = getUrl telegramToken method
        let content = new StringContent(Json.serialize payload, Encoding.UTF8, "application/json")
        task {
            let! response = httpClient.PostAsync(url, content)
            let! responseString = response.Content.ReadAsStringAsync()
            logger.LogDebug responseString
        }

    let sendMessage logger telegramToken (chatId : int64) (text : string) =
        {|
            ChatId = chatId
            Text = text
        |}
        |> makeRequest logger telegramToken "sendMessage"
