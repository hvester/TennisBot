namespace TennisBot

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.Serialization

type WebJobsExtensionStartup () =
    inherit FunctionsStartup ()
    override __.Configure(builder: IFunctionsHostBuilder) =      
        builder.Services.AddGiraffe() |> ignore
        builder.Services.AddSingleton<Json.IJsonSerializer>(
            NewtonsoftJsonSerializer(Json.serializerSettings)) |> ignore

[<assembly: FunctionsStartup(typeof<WebJobsExtensionStartup>)>]
do ()