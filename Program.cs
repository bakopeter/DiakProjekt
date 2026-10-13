using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Kapcsolati karakterlánc kiolvasása az appsettings.json-ból
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. DbContext regisztrálása (Ennek kötelezően a builder.Build() ELŐTT kell lennie!)
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(connStr));

// -------------------------------------------------------------
var app = builder.Build(); // A regisztrációknak ezen sor felett kell lenniük!
// -------------------------------------------------------------

app.MapGet("/", () => "OK");

// --- TESZT VÉGPONT ---
app.MapGet("/diak-teszt", (AppDbContext context) =>
{
    // 1. (Opcionális) Tesztadat hozzáadása, ha még üres az adatbázis
    if (!context.Students.Any())
    {
        context.Students.Add(new Student { FirstName = "xy" });
        context.SaveChanges();
    }

    // 2. A LINQ lekérdezés lefuttatása
    var result = context.Students
                        .Where(s => s.FirstName == "xy")
                        .ToList();

    // 3. Eredmény visszaadása JSON formátumban
    return result;
});

app.Run();