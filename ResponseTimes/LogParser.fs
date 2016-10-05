module LogParser

open System
open System.IO
open FSharp.Data
open FSharp.Text.RegexProvider

type Logs = CsvProvider<"logs.csv"> 
type Details = JsonProvider<"details.json", SampleIsList=true>
type ResponseTimeRegex = Regex< @"(?<=Request Finished in )((?<ResponseTime>\d+)(?= milliseconds))", noMethodPrefix = true >

type LogDetails = {
    ApplicationName : string;
    LoggerName : string;
    RequestedURL : Uri;
    HttpResponseStatusCode : int;
    HttpVerb : string;
    Message : string;
    TransactionId : Option<Guid>;
    ResponseTime : Option<int>;
}

let createDetailsJson () =
    let logs = Logs.Load("logs.csv")
    use outFile = new StreamWriter("details.json")

    outFile.WriteLine(sprintf "%s" "[")

    let details = 
        logs.Rows 
        |> Seq.take(100000)
        |> Seq.map(fun log -> log.Details)
        |> Seq.iter (fun detail -> outFile.WriteLine(sprintf "%s," detail))


    outFile.WriteLine(sprintf "%s" "]")
    outFile.Close ()

let convertLogToLogDetails (log : Logs.Row) =
    let details = Details.Parse(log.Details)
    let responseTime = ResponseTimeRegex().Match(details.Message).ResponseTime.Value
    {
        ApplicationName = log.ApplicationName;
        LoggerName = log.LoggerName;
        RequestedURL = new Uri(details.RequestedUrl);
        HttpResponseStatusCode = details.HttpResponseStatusCode;
        HttpVerb = details.HttpVerb;
        Message = details.Message;
        TransactionId = details.TransactionId;
        ResponseTime = match responseTime with
                       | "" -> None
                       | _ -> Some (Int32.Parse(responseTime))
    }


let parseCsv (fileName : string) =
    Logs.Load(fileName).Rows
    |> Seq.map(fun log -> convertLogToLogDetails log)