﻿namespace DG.XrmDefinitelyTyped

open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

open Utility
open IntermediateRepresentation


module internal InterpretOptionSetMetadata =

  let getLabelString (label:Label) =
    try
      label.UserLocalizedLabel.Label 
      |> Utility.sanitizeString
    with _ -> unknownLabel

  let getMetadataString (metadata:OptionSetMetadataBase) =
    getLabelString metadata.DisplayName
    |> fun name -> 
      if name <> unknownLabel then name
      else metadata.Name


  /// Interprets CRM OptionSetMetadata into intermediate type
  let interpretOptionSet (metadata:OptionSetMetadataBase) =
    match metadata with
    | :? OptionSetMetadata as osm ->

      let options =
        osm.Options
        |> Seq.map (fun opt ->
          { label = getLabelString opt.Label
            value = opt.Value.GetValueOrDefault() }) 

      let fixedOptionLabels =
        options
        |> Seq.fold (fun (countMap:Map<string,Option list>) op ->
          if countMap.ContainsKey op.label then
            countMap.Add(
              op.label, 
              { op with label = sprintf "%s_%d" op.label (countMap.[op.label].Length+1) } 
                :: countMap.[op.label]
            )
          else countMap.Add(op.label, [op])
        ) Map.empty
        |> Map.toArray |> Array.map snd |> List.concat 
        |> List.sortBy (fun op -> op.value) |> List.toArray

      { displayName = metadata.Name
        options = fixedOptionLabels }
      |> Some

    | :? BooleanOptionSetMetadata as bosm ->
      let options =
        [|  { label = getLabelString bosm.TrueOption.Label
              value = 1 }
            { label = getLabelString bosm.FalseOption.Label
              value = 0 } |]

      { displayName = metadata.Name
        options = options }
      |> Some

    | _ -> None