namespace TennisBot

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Microsoft.FSharpLu.Json

module Json =

    let serializerSettings =
        let contractResolver = DefaultContractResolver(NamingStrategy=SnakeCaseNamingStrategy())
        let settings =
            JsonSerializerSettings(
                ContractResolver=contractResolver,
                Formatting = Formatting.Indented)
        settings.Converters.Add(CompactUnionJsonConverter(true))
        settings

    let serialize object = JsonConvert.SerializeObject(object, serializerSettings)