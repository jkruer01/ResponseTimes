module LogParser

open System
open System.IO
open FSharp.Data
open FSharp.Text.RegexProvider
open Nessos.Streams

type Logs = CsvProvider<"sample.csv"> 
type Details = JsonProvider<"details.json", SampleIsList = true>
type ResponseTimeRegex = Regex< @"(?<=Request Finished in )((?<ResponseTime>\d+)(?= milliseconds))", noMethodPrefix = true >

/// Only create the matcher once rather than for every call
let private matcher = ResponseTimeRegex().Match

type LogDetails =
    { ApplicationName : string
      LoggerName : string
      RequestedURL : Uri
      HttpResponseStatusCode : int
      HttpVerb : string
      Message : string
      TransactionId : Guid option
      ResponseTime : int option }

let createDetailsJson () =
    let logs = Logs.Load "logs.csv"
    use outFile = new StreamWriter("details.json")

    outFile.WriteLine "["

    logs.Rows 
    |> Seq.take 100000
    |> Seq.map(fun log -> log.Details)
    |> Seq.iter(sprintf "%s," >> outFile.WriteLine)

    outFile.WriteLine "]"
    outFile.Close ()

let convertLogToLogDetails (log:Logs.Row) =
    let details = Details.Parse log.Details

    { ApplicationName = log.ApplicationName
      LoggerName = log.LoggerName
      RequestedURL = Uri details.RequestedUrl
      HttpResponseStatusCode = details.HttpResponseStatusCode
      HttpVerb = details.HttpVerb
      Message = details.Message
      TransactionId = details.TransactionId
      ResponseTime =
        match matcher(details.Message).ResponseTime.Value with
        | "" -> None
        | responseTime -> Some (int responseTime) }

let parseCsv (fileName:string) =
    Logs.Load(fileName).Rows
    |> ParStream.ofSeq
    |> ParStream.map convertLogToLogDetails