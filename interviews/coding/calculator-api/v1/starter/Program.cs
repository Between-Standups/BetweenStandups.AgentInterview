var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/calculate", () => Results.Json(
    new
    {
        error = "not_implemented",
        message = "Implement the calculator API."
    },
    statusCode: StatusCodes.Status501NotImplemented));

app.Run();
