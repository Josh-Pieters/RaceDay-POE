var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Makes the entry point visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }