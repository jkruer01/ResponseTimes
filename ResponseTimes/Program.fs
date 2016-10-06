open LogParser
open System
open System.IO
open Nessos.Streams

let removeEverythingAfterLast (separator : char) (input : string) =
    let parts = input.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)
    let numberOfItems = parts.Length
    let strSeparator = separator.ToString()

    parts
    |> Array.take (numberOfItems - 1)
    |> String.concat strSeparator
    |> fun result -> strSeparator + result

let cleanseRequestedUrl (url : string) =
    let url = url.ToLower()
                 .Replace("/sams", "")
                 .Replace("/service", "")
                 .Replace("/ess", "")

    if (url.Contains "api/v1/documents/" || url.Contains "api/v1/eftaccounts/") then removeEverythingAfterLast '/' url
    else url

let averageResponseTimesByRequestedURL =
    ParStream.choose(fun (logDetail:LogDetailsStruct) ->
        match logDetail.ResponseTime with
        | Some responseTime when responseTime > 0 -> Some(responseTime, logDetail.HttpVerb, logDetail.RequestedURL)
        | _ -> None)
    >> ParStream.groupBy(fun (_, verb, url) -> verb, cleanseRequestedUrl url.AbsolutePath)
    >> ParStream.map(fun (key, group) -> key, group |> Seq.averageBy(fun (responseTime, _, _) -> float responseTime))
    >> ParStream.sortByDescending snd

[<EntryPoint>]
let main _ = 
    let outputFile = "results.csv"
    use sw = new StreamWriter(outputFile)
    fprintfn sw "Method,Url,Avg ResponseTime (ms)"

    LogParser.parseCsv "logs.csv"
    |> averageResponseTimesByRequestedURL
    |> ParStream.iter(fun ((httpMethod, url), avgResponseTime) -> fprintfn sw "%s,%s,%.1f" httpMethod url avgResponseTime)

    sw.Close()

    printfn "Results saved to %s" outputFile
    0 // return an integer exit code
