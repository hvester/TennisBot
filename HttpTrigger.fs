namespace TennisBot

open System
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open Api
module Run =


    let errorHandler (ex : exn) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse
        >=> ServerErrors.INTERNAL_ERROR "Something went wrong :("


    [<FunctionName "TennisBot">]
    let run ([<HttpTrigger (AuthorizationLevel.Anonymous, Route = "{*any}")>] req : HttpRequest, context : ExecutionContext, logger : ILogger) =
        let hostingEnvironment = req.HttpContext.GetHostingEnvironment()
        hostingEnvironment.ContentRootPath <- context.FunctionAppDirectory
        let telegramToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN", EnvironmentVariableTarget.Process)
        let func = Some >> Task.FromResult
        { new Microsoft.AspNetCore.Mvc.IActionResult with
              member _.ExecuteResultAsync(ctx) = 
                  task {
                      try
                          return! (app logger telegramToken) func ctx.HttpContext :> Task
                      with exn ->
                          return! errorHandler exn logger func ctx.HttpContext :> Task
                  }
                  :> Task }