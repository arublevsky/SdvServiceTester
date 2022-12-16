using System;
using System.Collections.Generic;
using System.Dynamic;
using Contour;
using Contour.Transport.RabbitMQ;
using Gems.Hosting.Bootstrapper.ServiceBus;
using Gems.ServiceBus.Emitting;
using ServiceTester.Upload;

namespace ServiceTester
{
    class Program
    {
        static void Main(string[] args)
        {
            UploadUsersRecommendations();
        }

        #region get recommendations

        private static void GetUsersRecommendations()
        {
            using var bus = new BusFactory().Create(configurator =>
            {
                configurator.UsePayloadConverter(new JsonNetPayloadConverter());
                configurator.SetEndpoint("Users.Personal.Pipe");
                configurator.SetConnectionString("amqp://service:cp@docker.39.stage/cp");
                configurator
                    .Route("command.users.recommendations.find") // сюда подставляется значение из label
                    .WithConnectionString(
                        "amqp://service:cp@docker.39.stage/users") // нужный нам connection string
                    .WithAlias(
                        "command.users.recommendations.find") // он же key в конфигурации вызывающего компонента
                    .WithDefaultCallbackEndpoint(); // соответствует стандартному "callbackEndpoint": { "default": true } в конфигурации вызывающего компонента
            });
            var messageEmitter = new MessageEmitter(
                new DefaultRouteMapProvider(new Dictionary<Type, string>
                {
                    { typeof(FindCommand), ":command.users.recommendations.find" }
                }));
            messageEmitter.RegisterEndpoint(bus);

            var response = messageEmitter.RequestAsync<FindCommand, FindResponse>(
                new FindCommand
                {
                    UserId = 310102041980,
                    Model = "byResearchCenter"
                }).Result;

            Console.Write(response);
        }

        internal class FindResponse
        {
            public List<RecommItem> Recommendations { get; set; }
        }

        internal class RecommItem
        {
            public long UserId { get; set; }
            public Decimal Score { get; set; }
        }

        internal class FindCommand
        {
            public long UserId { get; set; }
            public string Model { get; set; }
        }

        #endregion

        #region upload recommendations

        private static void UploadUsersRecommendations()
        {
            using var bus = new BusFactory().Create(configurator =>
            {
                configurator.UsePayloadConverter(new JsonNetPayloadConverter());
                configurator.SetEndpoint("Users.Personal.Pipe");
                configurator.SetConnectionString("amqp://service:cp@docker.39.stage/annals");
                configurator
                    .Route("event.annals.uploaded..users.recommendations") // сюда подставляется значение из label
                    .WithConnectionString(
                        "amqp://service:cp@docker.39.stage/annals") // нужный нам connection string
                    .WithAlias(
                        "event.annals.uploaded..users.recommendations") // он же key в конфигурации вызывающего компонента
                    .WithDefaultCallbackEndpoint(); // соответствует стандартному "callbackEndpoint": { "default": true } в конфигурации вызывающего компонента
            });
            var messageEmitter = new MessageEmitter(
                new DefaultRouteMapProvider(new Dictionary<Type, string>
                {
                    { typeof(UploadMessage), ":event.annals.uploaded..users.recommendations" }
                }));
            messageEmitter.RegisterEndpoint(bus);

            messageEmitter.EmitAsync(
                new UploadMessage
                {
                    Recommendations = new UploadModel
                    {
                        Expiration = DateTime.Now,
                        ModelType = "UploadMessage",
                        Data = new []{new UploadData
                        {
                            Records = new List<UploadRecord>
                            {
                                new()
                                {
                                    Score = 777,
                                    RecommendedId = 999
                                }
                            }
                        }}
                    }
                }).GetAwaiter().GetResult();

            Console.Write("response");
        }

        #endregion

        #region get pltv

        private static void GetPltv()
        {
            using (var bus = new BusFactory().Create(configurator =>
                   {
                       configurator.UsePayloadConverter(new JsonNetPayloadConverter());
                       configurator.SetEndpoint("Users.Personal.Pipe");
                       configurator.SetConnectionString("amqp://service:cp@docker.39.stage/cp");
                       configurator
                           .Route("command.users.pltv.get") // сюда подставляется значение из label
                           .WithConnectionString(
                               "amqp://service:cp@docker.39.stage/users") // нужный нам connection string
                           .WithAlias("command.users.pltv.get") // он же key в конфигурации вызывающего компонента
                           .WithDefaultCallbackEndpoint(); // соответствует стандартному "callbackEndpoint": { "default": true } в конфигурации вызывающего компонента
                   }))
            {
                var messageEmitter = new MessageEmitter(
                    new DefaultRouteMapProvider(new Dictionary<Type, string>
                    {
                        { typeof(FindPltvCommand), ":command.users.pltv.get" }
                    }));
                messageEmitter.RegisterEndpoint(bus);

                var response = messageEmitter.RequestAsync<FindPltvCommand, FindPltvResponse>(
                    new FindPltvCommand
                    {
                        UserId = 200500200,
                    }).Result;

                Console.Write(response.Pltv?.ToString());
            }
        }

        internal class FindPltvResponse
        {
            public decimal? Pltv { get; set; }

            public decimal? PltvMax { get; set; }
        }

        internal class FindPltvCommand
        {
            public long UserId { get; set; }
        }

        #endregion
    }
}