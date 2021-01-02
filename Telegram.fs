namespace TennisBot

open System
open System.Text
open System.Net.Http
open Microsoft.Extensions.Logging
open FSharp.Reflection
open FSharp.Control.Tasks.V2.ContextInsensitive

module Telegram =
    open TennisBot

    [<RequireQualifiedAccess>]
    module Dto =

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
        type MessageEntity =
            {
                Type : string
                Offset : int
                Length : int
            }

        [<CLIMutable>]
        type Message =
            {
                MessageId : int64
                Text : string option
                From : User option
                Chat : Chat
                Entities : MessageEntity array option
            }

        [<CLIMutable>]
        type Update =
            {
                UpdateId : int
                Message : Message option
            }


        let private commandNameMapping =
            FSharpType.GetUnionCases typeof<Command>
            |> Array.map (fun unionCaseInfo ->
                let caseName = unionCaseInfo.Name.ToLower()
                (caseName, FSharpValue.MakeUnion(unionCaseInfo, [||]) :?> Command))
            |> dict


        let parseCommand (str : string) =
            match commandNameMapping.TryGetValue(str.Substring(1).Replace("_", "")) with
            | false, _ -> None
            | true, command -> Some command


        let toDomainMessage message =
            let botCommandEntityOpt =
                message.Entities
                |> Option.bind (fun entities ->
                    entities
                    |> Array.tryFind (fun entity -> entity.Type = "bot_command"))
            match message.Text, botCommandEntityOpt with
            | Some text, Some entity ->
                let commandString = text.Substring(entity.Offset, entity.Length)
                match parseCommand commandString with
                | None -> Error (sprintf "Unknown command: %s" commandString)
                | Some command -> Ok (Command command)
            | Some text, None ->
                Ok (Text text)
            | _ ->
                Error (sprintf "Cannot convert to domain message:\n%s" (string message))



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
            ParseMode = "HTML"
        |}
        |> makeRequest logger telegramToken "sendMessage"
