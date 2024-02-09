using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace MediatrConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // create mediatr instance
            var services = new ServiceCollection()
                // add MediatR and register all handlers in the current assembly
                .AddMediatR((c) =>
                {
                    c.RegisterServicesFromAssemblyContaining<Program>();
                })

               // register handlers manually
               //.AddScoped<IRequestHandler<PingPong.Ping, PingPong.Pong>, PingPong.PingHandler>()

               // register misc. services
               .AddScoped<TextWriter, StringWriter>();

            var container = services.BuildServiceProvider();
            var mediator = container.GetRequiredService<IMediator>();

            // send a request for ping and pong
            var pong = await mediator.Send(new PingPong.Ping() { Message = "Hello" });
            Assert.That(pong.Message, Is.EqualTo("Hello Pong"));

            // lets validate something
            var isValid = await mediator.Send(new IsPrimeValidateRequest() { Value = 10 });
            Assert.That(isValid, Is.False);
            isValid = await mediator.Send(new IsPrimeValidateRequest() { Value = 13 });
            Assert.That(isValid, Is.True);

            // lets publish a notification for multiple receivers
            await mediator.Publish(new ChangeStateNotification() { State = "open" });
        }
    }

    internal class ChangeStateNotification : INotification
    {
        public string State { get; set; } = null!;
    }

    internal class ChangeStateNotificationHandler : INotificationHandler<ChangeStateNotification>
    {
        public Task Handle(ChangeStateNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"State changed to {notification.State}");
            return Task.CompletedTask;
        }
    }

    internal class AnotherChangeStateNotificationHandler : INotificationHandler<ChangeStateNotification>
    {
        public Task Handle(ChangeStateNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Another State changed to {notification.State}");
            return Task.CompletedTask;
        }
    }



    internal class IsPrimeValidateRequest : IRequest<bool>
    {
        public int Value { get; set; }
    }

    internal class IsPrimeValidateRequestHandler : IRequestHandler<IsPrimeValidateRequest, bool>
    {
        public Task<bool> Handle(IsPrimeValidateRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsPrime(request.Value));
        }

        // check if an input is a prime number using sqrt
        private bool IsPrime(int n)
        {
            if (n <= 1) return false;
            if (n <= 3) return true;

            if (n % 2 == 0 || n % 3 == 0) return false;

            for (int i = 5; i * i <= n; i += 6)
            {
                if (n % i == 0 || n % (i + 2) == 0) return false;
            }

            return true;
        }

    }

    public static class PingPong
    {
        public class Pong
        {
            public string Message { get; set; }
        }

        public class Ping : IRequest<Pong>
        {
            public string Message { get; set; }
        }

        public class PingHandler : IRequestHandler<Ping, Pong>
        {
            private readonly TextWriter _writer;

            // DI works like charm
            public PingHandler(TextWriter writer)
            {
                _writer = writer;
            }

            public async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            {
                await _writer.WriteLineAsync($"--- Handled Ping: {request.Message}");
                return new Pong { Message = request.Message + " Pong" };
            }
        }

        public class ConsolePingHandler : IRequestHandler<Ping, Pong>
        {
            // DI works like charm
            public ConsolePingHandler(TextWriter writer)
            {
            }

            public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"Handled Ping: {request.Message}");
                return Task.FromResult(new Pong { Message = request.Message + " Pong" });
            }
        }
    }

}
