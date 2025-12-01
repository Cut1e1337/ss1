using Microsoft.EntityFrameworkCore;
using ss1.Data;
using ss1.Interfaces;
using ss1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        // Для інтеграційних тестів
        options.UseInMemoryDatabase("TestDb");
    }
    else
    {
        // Для звичайного запуску API
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