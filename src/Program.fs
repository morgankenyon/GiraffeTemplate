namespace GiraffeTemplate

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Http

// ---------------------------------
// Models
// ---------------------------------
module Models =
    type NewUser =
        {
            FirstName : string
            LastName : string
            Phone: string
        }

    [<CLIMutable>]
    type User =
        {
            UserId : int32
            FirstName : string
            LastName : string
            Phone: string
        }

// ---------------------------------
// Views
// ---------------------------------

//module Views =
//    open Giraffe.ViewEngine

//    let layout (content: XmlNode list) =
//        html [] [
//            head [] [
//                title []  [ encodedText "GiraffeTemplate" ]
//            ]
//            body [] content
//        ]

//    let partial () =
//        h1 [] [ encodedText "GiraffeTemplate" ]

//    let index (model : Message) =
//        [
//            partial()
//            p [] [ encodedText model.Text ]
//        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

module Handlers =
    open Models
    let getAllUsersHandler () =
        let users = [
            {
                UserId = 1
                FirstName = "Tod"
                LastName = "Bo"
                Phone = "2342"
            }
            {
                UserId = 2
                FirstName = "Harry"
                LastName = "Thomas"
                Phone = "211342"
            }
        ]
        json users

    let insertUserHandler : HttpHandler =
        fun (next : HttpFunc) (ctx: HttpContext) ->
            task {
                //bind json
                let! newUser = ctx.BindJsonAsync<NewUser>()

                //return! Successful.OK newUser next ctx
                return! ctx.WriteJsonAsync newUser
            }



module Api =
    open Handlers
    let webApp =
        choose [
            GET >=>
                choose [
                    route "/user" >=> getAllUsersHandler()
                ]
            POST >=>
                choose [
                    route "/user" >=> insertUserHandler
                ]
            setStatusCode 404 >=> text "Not Found" ]

    // ---------------------------------
    // Error handler
    // ---------------------------------

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message

    // ---------------------------------
    // Config and Main
    // ---------------------------------

    let configureCors (builder : CorsPolicyBuilder) =
        builder
            .WithOrigins(
                "http://localhost:5000",
                "https://localhost:5001")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
        (match env.IsDevelopment() with
        | true  ->
            app.UseDeveloperExceptionPage()
        | false ->
            app .UseGiraffeErrorHandler(errorHandler)
                .UseHttpsRedirection())
            .UseCors(configureCors)
            .UseStaticFiles()
            .UseGiraffe(webApp)

    let configureServices (services : IServiceCollection) =
        services.AddCors()    |> ignore
        services.AddGiraffe() |> ignore

    let configureLogging (builder : ILoggingBuilder) =
        builder.AddConsole()
               .AddDebug() |> ignore

    [<EntryPoint>]
    let main args =
        let contentRoot = Directory.GetCurrentDirectory()
        //let webRoot     = Path.Combine(contentRoot, "WebRoot")
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    webHostBuilder
                        .UseContentRoot(contentRoot)
                        //.UseWebRoot(webRoot)
                        .Configure(Action<IApplicationBuilder> configureApp)
                        .ConfigureServices(configureServices)
                        .ConfigureLogging(configureLogging)
                        |> ignore)
            .Build()
            .Run()
        0