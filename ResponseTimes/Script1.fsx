#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
#r "../packages/FSharp.Text.RegexProvider.1.0.0/lib/net40/FSharp.Text.RegexProvider.dll"
#r "../packages/Streams.0.4.1/lib/net45/Streams.dll"

#load "LogParser.fs"

open LogParser
open System.Diagnostics
open Nessos.Streams
open System

#time

let timeIt code =
    let sw = Stopwatch.StartNew()
    code() |> ignore
    sw.Stop()
    sw

let timeItAverage tries code =
    [ for i in 1 .. tries ->
        printfn "Try #%d..." i
        let time = timeIt code
        printfn "took %O" time.Elapsed
        time.Elapsed.TotalSeconds ]
    |> List.average

timeItAverage 5 (fun () -> LogParser.parseCsv "logs.csv" |> ParStream.take 250000 |> ParStream.toArray)

LogParser.parseCsv "logs.csv" 

// Original: 3.10156202
// Array: 2.77468476
// Array.Parallel: 1.82620238
// Stream: 1.93995966
// ParStream: 1.89536202
