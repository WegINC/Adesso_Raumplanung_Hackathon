using FastEndpoints;

namespace Roomy.Api.Endpoints.Greeting;

public class GreetingEndpoint : Endpoint<GreetingRequest, GreetingResponse>
{
    public override void Configure()
    {
        Get("/api/greeting/{Name}");
        // Require authentication
    }

    public override Task HandleAsync(GreetingRequest req, CancellationToken ct)
    {
        var name = string.IsNullOrWhiteSpace(req.Name) ? "World" : req.Name;

        Response = new GreetingResponse
        {
            Message = $"Hello, {name}! Welcome to Roomy - Room Reservation System",
            Timestamp = DateTime.UtcNow
        };

        return Task.CompletedTask;
    }
}
