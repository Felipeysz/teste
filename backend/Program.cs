using backend.Services;
using backend.BusinessRules;
using backend.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

// Configurações: Azure SQL, JWT, Swagger e rotas customizadas
builder.Services.AddBancoAzure(builder.Configuration)
    .AddJwtConfiguration(builder.Configuration)
    .AddSwaggerConfiguration()
    .AddCustomRouting();

// DI serviços e repositórios (sem interfaces)
builder.Services.AddScoped<IUserService, UserService>().AddScoped<UserBusinessRules>();


var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.MapCustomRoutes();
app.Run();

public partial class Program { }
