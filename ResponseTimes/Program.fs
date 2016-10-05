// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open LogParser
open System
open System.IO

let removeEverythingAfterLast (separator : char) (input : string) =
    let parts = input.Split([|separator|], StringSplitOptions.RemoveEmptyEntries)
    let numberOfItems = parts.Length 
    let strSeparator = separator.ToString()

    Seq.take (numberOfItems - 1) parts
    |> Seq.toList
    |> String.concat strSeparator
    |> (fun result -> strSeparator + result)

let cleanseRequestedUrl (url : string) =
    let lower = url.ToLower()
                    .Replace("/sams", "")
                    .Replace("/service", "")
                    .Replace("/ess", "")

    if (lower.Contains "api/v1/documents/" || lower.Contains "api/v1/eftaccounts/")  then
        removeEverythingAfterLast '/' lower
    else lower

let averageResponseTimesByRequestedURL logDetails =
    Seq.filter(fun logDetail -> logDetail.ResponseTime.IsSome && logDetail.ResponseTime.Value > 0) logDetails
    |> Seq.groupBy(fun logDetail -> sprintf "%s,%s" logDetail.HttpVerb (cleanseRequestedUrl logDetail.RequestedURL.AbsolutePath)) 
    |> Seq.map(fun (key, group) -> key, group |> Seq.map(fun logDetail -> float logDetail.ResponseTime.Value) |> Seq.average)
    |> Seq.sortByDescending(fun (key, value) -> value)




[<EntryPoint>]
let main argv = 
    let outputFile = "results.csv"
    use sw = new StreamWriter(outputFile)
    fprintfn sw "Method,Url,Avg ResposneTime (ms)"

    LogParser.parseCsv "logs.csv"
    |> averageResponseTimesByRequestedURL
    |> Seq.iter(fun (url, avgResponseTime) -> fprintfn sw "%s,%.1f" url avgResponseTime)

    sw.Close();

    printfn "Results saved to %s" outputFile
    printfn "Press any key to continue..."
    let input = Console.ReadLine()
    0 // return an integer exit code
