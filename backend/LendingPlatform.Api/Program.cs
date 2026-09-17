using LendingPlatform.Api.Data;
using LendingPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LendingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LendingDatabase")
                      ?? "Data Source=lending.db"));

builder.Services.AddScoped<ILoanDecisionService, LoanDecisionService>();
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Small assessment project: create the SQLite file on first run instead of shipping
// migrations. A production system would use EF migrations run as a deploy step.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LendingDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Lending Platform API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors(FrontendCors);
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
