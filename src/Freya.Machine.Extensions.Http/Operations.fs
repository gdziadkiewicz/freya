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

[<RequireQualifiedAccess>]
module internal Freya.Machine.Extensions.Http.Operations

open System
open Arachne.Http
open Freya.Core
open Freya.Core.Operators
open Freya.Lenses.Http

(* Helpers *)

let private allow =
    Allow >> Some >> (.=) Response.Headers.allow_

let private date =
    Date.Date >> Some >> (.=) Response.Headers.date_ <| DateTime.UtcNow

let private eTag =
    Option.map ETag >> (.=) Response.Headers.eTag_

let private expires =
    Option.map Expires >> (.=) Response.Headers.expires_

let private lastModified =
    Option.map LastModified >> (.=) Response.Headers.lastModified_

let private location =
    Option.map Location >> (.=) Response.Headers.location_

let private phrase =
    Some >> (.=) Response.reasonPhrase_

let private status =
    Some >> (.=) Response.statusCode_

(* Operations *)

let private accepted =
        status 202
     *> phrase "Accepted"
     *> date

let private badRequest =
        status 400
     *> phrase "Bad Request"
     *> date

let private conflict =
        status 409
     *> phrase "Conflict"
     *> date

let private created uri =
        status 201
     *> phrase "Created"
     *> date
     *> location uri

let private forbidden =
        status 403
     *> phrase "Forbidden"
     *> date

let private gone =
        status 410 
     *> phrase "Gone"
     *> date

let private methodNotAllowed allowedMethods =
        status 405
     *> phrase "Method Not Allowed"
     *> allow allowedMethods
     *> date

let private movedPermanently uri =
        status 301
     *> phrase "Moved Permanently"
     *> date
     *> location uri

let private movedTemporarily uri =
        status 307
     *> phrase "Moved Temporarily"
     *> date
     *> location uri

let private multipleRepresentations =
        status 310
     *> phrase "Multiple Representations"
     *> date

let private noContent =
        status 204
     *> phrase "No Content"
     *> date

let private notAcceptable =
        status 406
     *> phrase "Not Acceptable"
     *> date

let private notFound =
        status 404
     *> phrase "Not Found"
     *> date

let private notImplemented =
        status 501
     *> phrase "Not Implemented"
     *> date

let private notModified modificationDate entityTag expiryDate =
        status 304
     *> phrase "Not Modified"
     *> lastModified modificationDate
     *> date
     *> eTag entityTag
     *> expires expiryDate

let private ok modificationDate entityTag expiryDate =
        status 200
     *> phrase "OK"
     *> lastModified modificationDate
     *> date
     *> eTag entityTag
     *> expires expiryDate

let private options modificationDate entityTag expiryDate =
        status 200
     *> phrase "Options"
     *> lastModified modificationDate
     *> date
     *> eTag entityTag
     *> expires expiryDate

let private preconditionFailed =
        status 412
     *> phrase "Precondition Failed"
     *> date

let private requestEntityTooLarge =
        status 413
     *> phrase "Request Entity Too Large"
     *> date

let private seeOther uri =
        status 303
     *> phrase "See Other"
     *> date
     *> location uri

let private serviceUnavailable =
        status 503
     *> phrase "Service Unavailable"
     *> date

let private unauthorized =
        status 401
     *> phrase "Unauthorized"
     *> date

let private unknownMethod =
        status 501
     *> phrase "Unknown Method"
     *> date

let private unprocessableEntity =
        status 422
     *> phrase "Unprocessable Entity"
     *> date

let private unsupportedMediaType =
        status 415
     *> phrase "UnsupportedMediaType"
     *> date

let private uriTooLong =
        status 414
     *> phrase "URI Too Long"
     *> date

(* System operations *)

open Freya.Machine

module internal SystemOperation =

    let private systemOperation f =
        Some (Compile (fun config ->
            Compiled (Unary (f config), unconfigurable)))

    let private getMappedOrDefault f defaultValue key =
           Configuration.get key
        >> Option.map f
        >> Option.orElse (defaultValue)

    let private getOptional key =
        getMappedOrDefault ((<!>) Some) (Freya.init None) key

    let private eTag =
        getOptional Properties.ETag

    let private expires =
        getOptional Properties.Expires

    let private lastModified =
        getOptional Properties.LastModified

    let private location =
        getOptional Properties.Location

    let private methodsSupported =
        getMappedOrDefault id Defaults.methodsSupported Properties.MethodsSupported

    let created =
            location
        >=> created
         |> systemOperation

    let methodNotAllowed =
            methodsSupported
        >=> methodNotAllowed
         |> systemOperation

    let movedPermanently =
            location
        >=> movedPermanently
         |> systemOperation

    let movedTemporarily =
            location
        >=> movedTemporarily
         |> systemOperation

    let notModified =
        (fun config ->
            notModified
        <!> lastModified config
        <*> eTag config
        <*> expires config
        >>= id)
         |> systemOperation

    let ok =
        (fun config ->
            ok
        <!> lastModified config
        <*> eTag config
        <*> expires config
        >>= id)
         |> systemOperation

    let options =
        (fun config ->
            options
        <!> lastModified config
        <*> eTag config
        <*> expires config
        >>= id)
         |> systemOperation

    let seeOther =
            location
        >=> seeOther
         |> systemOperation

    let noConfig x = systemOperation (fun _ -> x)

(* Graph *)

open Freya.Machine.Operators

let operations =
    [ Operation Operations.Accepted                    =.        SystemOperation.noConfig accepted
      Operation Operations.BadRequest                  =.        SystemOperation.noConfig badRequest
      Operation Operations.Conflict                    =.        SystemOperation.noConfig conflict
      Operation Operations.Created                     =.        SystemOperation.created
      Operation Operations.Forbidden                   =.        SystemOperation.noConfig forbidden
      Operation Operations.Gone                        =.        SystemOperation.noConfig gone
      Operation Operations.MethodNotAllowed            =.        SystemOperation.methodNotAllowed
      Operation Operations.MovedPermanently            =.        SystemOperation.movedPermanently
      Operation Operations.MovedTemporarily            =.        SystemOperation.movedTemporarily
      Operation Operations.MultipleRepresentations     =.        SystemOperation.noConfig multipleRepresentations
      Operation Operations.NoContent                   =.        SystemOperation.noConfig noContent
      Operation Operations.NotAcceptable               =.        SystemOperation.noConfig notAcceptable
      Operation Operations.NotFound                    =.        SystemOperation.noConfig notFound
      Operation Operations.NotImplemented              =.        SystemOperation.noConfig notImplemented
      Operation Operations.NotModified                 =.        SystemOperation.notModified
      Operation Operations.OK                          =.        SystemOperation.ok
      Operation Operations.Options                     =.        SystemOperation.options
      Operation Operations.PreconditionFailed          =.        SystemOperation.noConfig preconditionFailed
      Operation Operations.RequestEntityTooLarge       =.        SystemOperation.noConfig requestEntityTooLarge
      Operation Operations.SeeOther                    =.        SystemOperation.seeOther
      Operation Operations.ServiceUnavailable          =.        SystemOperation.noConfig serviceUnavailable
      Operation Operations.Unauthorized                =.        SystemOperation.noConfig unauthorized
      Operation Operations.UnknownMethod               =.        SystemOperation.noConfig unknownMethod
      Operation Operations.UnprocessableEntity         =.        SystemOperation.noConfig unprocessableEntity
      Operation Operations.UnsupportedMediaType        =.        SystemOperation.noConfig unsupportedMediaType
      Operation Operations.UriTooLong                  =.        SystemOperation.noConfig uriTooLong

      Operation Operations.Accepted                    >.        Operation Handlers.Accepted
      Operation Operations.BadRequest                  >.        Operation Handlers.BadRequest
      Operation Operations.Conflict                    >.        Operation Handlers.Conflict
      Operation Operations.Created                     >.        Operation Handlers.Created
      Operation Operations.Forbidden                   >.        Operation Handlers.Forbidden
      Operation Operations.Gone                        >.        Operation Handlers.Gone
      Operation Operations.MethodNotAllowed            >.        Operation Handlers.MethodNotAllowed
      Operation Operations.MovedPermanently            >.        Operation Handlers.MovedPermanently
      Operation Operations.MovedTemporarily            >.        Operation Handlers.MovedTemporarily
      Operation Operations.MultipleRepresentations     >.        Operation Handlers.MultipleRepresentations
      Operation Operations.NoContent                   >.        Operation Handlers.NoContent
      Operation Operations.NotAcceptable               >.        Operation Handlers.NotAcceptable
      Operation Operations.NotFound                    >.        Operation Handlers.NotFound
      Operation Operations.NotImplemented              >.        Operation Handlers.NotImplemented
      Operation Operations.NotModified                 >.        Operation Handlers.NotModified
      Operation Operations.OK                          >.        Operation Handlers.OK
      Operation Operations.Options                     >.        Operation Handlers.Options
      Operation Operations.PreconditionFailed          >.        Operation Handlers.PreconditionFailed
      Operation Operations.RequestEntityTooLarge       >.        Operation Handlers.RequestEntityTooLarge
      Operation Operations.SeeOther                    >.        Operation Handlers.SeeOther
      Operation Operations.ServiceUnavailable          >.        Operation Handlers.ServiceUnavailable
      Operation Operations.Unauthorized                >.        Operation Handlers.Unauthorized
      Operation Operations.UnknownMethod               >.        Operation Handlers.UnknownMethod
      Operation Operations.UnprocessableEntity         >.        Operation Handlers.UnprocessableEntity
      Operation Operations.UnsupportedMediaType        >.        Operation Handlers.UnsupportedMediaType
      Operation Operations.UriTooLong                  >.        Operation Handlers.UriTooLong ]