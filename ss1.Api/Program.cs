using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ss1.Api.Filters;
using ss1.Data;
using ss1.Interfaces;
using ss1.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + наш глобальний фільтр валідації
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });

// Реєструємо FluentValidation-валідатори (коли вони з’являться)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("TestDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// E-mail сервіс
builder.Services.AddScoped<IEmailSender, EmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
