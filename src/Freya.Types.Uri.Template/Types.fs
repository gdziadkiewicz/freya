﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2014
//
//    Ryan Riley (@panesofglass) and Andrew Cherry (@kolektiv)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//----------------------------------------------------------------------------

module Freya.Types.Uri.Template

open Freya.Types
open Freya.Types.Formatting
open Freya.Types.Parsing
open FParsec

(* RFC 6570

   Types, parsers and formatters implemented to mirror the specification of 
   URI Template semantics as defined in RFC 6570.

   Taken from [http://tools.ietf.org/html/rfc6570] *)

(* Grammar

   NOTE: We do not currently support either PCT encoded characters (a
   must fix) or international characters (supporting IRIs - this may
   be supported in future). *)

let private literal =
    Set.unionMany [
        set [ char 0x21 ]
        charRange 0x23 0x24
        set [ char 0x26 ]
        charRange 0x28 0x3b
        set [ char 0x3d ]
        charRange 0x3f 0x5b
        set [ char 0x5d ]
        set [ char 0x5f ]
        charRange 0x61 0x7a
        set [ char 0x7e ] ]

let private varchar =
    Set.unionMany [
        Grammar.alpha
        Grammar.digit
        set [ '_' ] ]

(* Template

   Taken from RFC 6570, Section 2 Syntax
   See [http://tools.ietf.org/html/rfc6570#section-2] *)

type UriTemplate =
    | UriTemplate of UriTemplatePart list

    static member Mapping =

        let uriTemplateP =
            many1 UriTemplatePart.Mapping.Parse |>> UriTemplate

        let uriTemplateF =
            function | UriTemplate u -> join UriTemplatePart.Mapping.Format id u

        { Parse = uriTemplateP
          Format = uriTemplateF }

    static member internal Rendering =

        let uriTemplateR (data: UriTemplateData) =
            function | UriTemplate p -> join (UriTemplatePart.Rendering.Render data) id p

        { Render = uriTemplateR }

    static member Format =
        Formatting.format UriTemplate.Mapping.Format

    static member Parse =
        Parsing.parse UriTemplate.Mapping.Parse

    static member TryParse =
        Parsing.tryParse UriTemplate.Mapping.Parse

    override x.ToString () =
        UriTemplate.Format x

    member x.TryRender data =
        Uri.TryParse (Rendering.render UriTemplate.Rendering.Render data x)

and UriTemplatePart =
    | Literal of Literal
    | Expression of Expression

    static member Mapping =

        let uriTemplatePartP =
            (Expression.Mapping.Parse |>> Expression) <|> (Literal.Mapping.Parse |>> Literal)

        let uriTemplatePartF =
            function | Literal l -> Literal.Mapping.Format l
                     | Expression e -> Expression.Mapping.Format e

        { Parse = uriTemplatePartP
          Format = uriTemplatePartF }

    static member internal Rendering =

        let uriTemplatePartR data =
            function | Literal l -> Literal.Rendering.Render data l
                     | Expression e-> Expression.Rendering.Render data e

        { Render = uriTemplatePartR }

and Literal =
    | Literal of string

    static member Mapping =

        let literalP =
            many1Satisfy ((?>) literal) |>> Literal.Literal

        let literalF =
            function | Literal l -> append l

        { Parse = literalP
          Format = literalF }

    static member Rendering =

        let literalR _ =
            function | Literal l -> append l

        { Render = literalR }

and Expression =
    | Expression of Operator option * VariableList

    static member Mapping =

        let expressionP =
            between 
                (skipChar '{') (skipChar '}') 
                (opt Operator.Mapping.Parse .>>. VariableList.Mapping.Parse)
                |>> Expression

        let expressionF =
            function | Expression (Some o, v) ->
                           append "{"
                        >> Operator.Mapping.Format o
                        >> VariableList.Mapping.Format v
                        >> append "}"
                     | Expression (_, v) ->
                           append "{"
                        >> VariableList.Mapping.Format v
                        >> append "}"

        { Parse = expressionP
          Format = expressionF }

    static member Rendering =

        let expressionR _ =
            function | Expression (Some o, v) -> append "opexp"
                     | Expression (_, v) -> append "exp"

        { Render = expressionR }

(* Operators

   Taken from RFC 6570, Section 2.2 Expressions
   See [http://tools.ietf.org/html/rfc6570#section-2.2] *)

and Operator =
    | Level2 of OperatorLevel2
    | Level3 of OperatorLevel3
    | Reserved of OperatorReserved

    static member Mapping =

        let operatorP =
            choice [
                OperatorLevel2.Mapping.Parse |>> Level2
                OperatorLevel3.Mapping.Parse |>> Level3
                OperatorReserved.Mapping.Parse |>> Reserved ]

        let operatorF =
            function | Level2 o -> OperatorLevel2.Mapping.Format o
                     | Level3 o -> OperatorLevel3.Mapping.Format o
                     | Reserved o -> OperatorReserved.Mapping.Format o

        { Parse = operatorP
          Format = operatorF }

and OperatorLevel2 =
    | Plus
    | Hash

    static member Mapping =

        let operatorLevel2P =
            choice [
                skipChar '+' >>% Plus
                skipChar '#' >>% Hash ]

        let operatorLevel2F =
            function | Plus -> append "+"
                     | Hash -> append "#"

        { Parse = operatorLevel2P
          Format = operatorLevel2F }

and OperatorLevel3 =
    | Dot
    | Slash
    | SemiColon
    | Question
    | Ampersand

    static member Mapping =

        let operatorLevel3P =
            choice [
                skipChar '.' >>% Dot
                skipChar '/' >>% Slash
                skipChar ';' >>% SemiColon
                skipChar '?' >>% Question
                skipChar '&' >>% Ampersand ]

        let operatorLevel3F =
            function | Dot -> append "."
                     | Slash -> append "/"
                     | SemiColon -> append ";"
                     | Question -> append "?"
                     | Ampersand -> append "&"

        { Parse = operatorLevel3P
          Format = operatorLevel3F }

and OperatorReserved =
    | Equals
    | Comma
    | Exclamation
    | At
    | Pipe

    static member Mapping =

        let operatorReservedP =
            choice [
                skipChar '=' >>% Equals
                skipChar ',' >>% Comma
                skipChar '!' >>% Exclamation
                skipChar '@' >>% At
                skipChar '|' >>% Pipe ]

        let operatorReservedF =
            function | Equals -> append "="
                     | Comma -> append ","
                     | Exclamation -> append "!"
                     | At -> append "@"
                     | Pipe -> append "!"

        { Parse = operatorReservedP
          Format = operatorReservedF }

(* Variables

   Taken from RFC 6570, Section 2.3 Variables
   See [http://tools.ietf.org/html/rfc6570#section-2.3] *)

and VariableList =
    | VariableList of VariableSpec list

    static member Mapping =

        let variableListP =
            sepBy1 VariableSpec.Mapping.Parse (skipChar ',')
            |>> VariableList

        let variableListF =
            function | VariableList v -> join VariableSpec.Mapping.Format commaF v

        { Parse = variableListP
          Format = variableListF }

and VariableSpec =
    | VariableSpec of VariableName * Modifier option

    static member Mapping =

        let variableSpecP =
            VariableName.Mapping.Parse .>>. opt Modifier.Mapping.Parse
            |>> VariableSpec

        let variableSpecF =
            function | VariableSpec (name, Some m) ->
                           VariableName.Mapping.Format name
                        >> Modifier.Mapping.Format m
                     | VariableSpec (name, _) ->
                        VariableName.Mapping.Format name

        { Parse = variableSpecP
          Format = variableSpecF }

and VariableName =
    | VariableName of string

    static member Mapping =

        let varcharsP =
            many1Satisfy ((?>) varchar)

        let variableNamePartP =
            opt (pstring ".") .>>. varcharsP
            |>> function | (Some s1, s2) -> s1 + s2
                         | (_, s2) -> s2

        let variableNameP =
            varcharsP .>>. many variableNamePartP
            |>> (fun (c, s) ->
                (String.concat "" >> VariableName) (c :: s))

        let variableNameF =
            function | VariableName n -> append n

        { Parse = variableNameP
          Format = variableNameF }

(* Modifiers

   Taken from RFC 6570, Section 2.4 Value Modifiers
   See [http://tools.ietf.org/html/rfc6570#section-2.4] *)

and Modifier =
    | Level4 of ModifierLevel4

    static member Mapping =

        let modifierP =
            ModifierLevel4.Mapping.Parse |>> Level4

        let modifierF =
            function | Level4 m -> ModifierLevel4.Mapping.Format m

        { Parse = modifierP
          Format = modifierF }

and ModifierLevel4 =
    | Prefix of int
    | Explode

    static member Mapping =

        let modifierLevel4P =
            choice [
                skipChar ':' >>. pint32 |>> Prefix
                skipChar '*' >>% Explode ]

        let modifierLevel4F =
            function | Prefix i -> appendf1 ":{0}" i
                     | Explode -> append "*"

        { Parse = modifierLevel4P
          Format = modifierLevel4F }

(* Data

   Types representing data which may be rendered or extracted
   using UriTemplates. *)

and UriTemplateData =
    | UriTemplateData of Map<string, UriTemplateDataItem>

and UriTemplateDataItem =
    | Atom of string
    | List of string list
    | Map of Map<string, string>
