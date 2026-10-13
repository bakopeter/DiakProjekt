# Útmutató: EF Core projekt készítése Visual Studio 2022-ben Parancssor (CLI) Használatával

Ez az útmutató lépésről lépésre mutatja be egy Entity Framework Core-ra épülő C# projekt létrehozását .NET 8 keretrendszeren, Visual Studio 2022 környezetben.

---

## 1. Projekt létrehozása parancssorból

Nyiss egy **Parancssort (CMD)** vagy **PowerShell** ablakot, majd hozd létre az új **ASP.NET Core Web API** projektet:

```bash
dotnet new webapi -n DiakProjekt -f net8.0
cd DiakProjekt
```

---

## 2. Telepítések és csomagfüggőségek

### A) A globális EF Core CLI eszköz telepítése
A migrációk parancssoros futtatásához szükséges az `ef` eszköz (amennyiben már telepítve van akár v10-es verzióval, az is tökéletesen együttműködik a .NET 8-as projekttel):

```bash
dotnet tool install --global dotnet-ef
```
*(Ha frissíteni szeretnéd: `dotnet tool update --global dotnet-ef`)*

### B) A szükséges NuGet csomagok telepítése
A verzió-kompatibilitási hibák és a *Package Downgrade* figyelmeztetés elkerülése érdekében egységesen **8.0.8** verziójú csomagokat használunk:

```bash
# EF Core alapcsomag
dotnet add package Microsoft.EntityFrameworkCore -v 8.0.8

# Adatbázis-szolgáltató (MySQL példa)
dotnet add package MySql.EntityFrameworkCore -v 8.0.8

# Tervező és migrációs eszközök a projekthez
dotnet add package Microsoft.EntityFrameworkCore.Design -v 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Tools -v 8.0.8
```

*(Megjegyzés: Ha SQL Server-t használsz, a MySQL csomag helyett a `Microsoft.EntityFrameworkCore.SqlServer -v 8.0.8` csomagot telepítsd!)*

### C) A projekt megnyitása Visual Studio 2022-ben
A projekt mappájában állva futtasd az alábbi parancsot:

```bash
start DiakProjekt.csproj
```

---

## 3. Adatbázis-kapcsolat beállítása (`appsettings.json`)

Nyisd meg az `appsettings.json` fájlt a Visual Studio-ban, és add meg az adatbázis elérhetőségét:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MintaDb;User=root;Password=secret;"
  }
}
```

---

## 4. Entitások (Osztályok) létrehozása DataAnnotations dekorátorokkal

Hozz létre egy `Models.cs` fájlt a projektben a C# entitások meghatározásához. A Code-First megközelítés alapján az EF Core ezekből fogja felépíteni az adatbázis-táblákat.

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Táblanév felülírása az adatbázisban
[Table("Diakok")]
public class Student
{
    [Key] // Elsődleges kulcs
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment mező
    public int StudentId { get; set; }

    [Required] // NOT NULL megszorítás
    [MaxLength(50)] // VARCHAR(50)
    [Column("KeresztNev", TypeName = "varchar(50)")] // Testreszabott SQL oszlopnév
    public string FirstName { get; set; } = null!;

    [NotMapped] // Nem jön létre oszlop az adatbázisban
    public string FullName => $"{FirstName}";

    // Kapcsolótábla navigációs tulajdonság (N:M kapcsolat)
    public List<StudentCourse> StudentCourses { get; set; } = new();
}

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; } = null!;

    public List<StudentCourse> CourseCourses { get; set; } = new();
}

// Kapcsolótábla az N:M kapcsolathoz
public class StudentCourse
{
    [ForeignKey("Student")] // Idegen kulcs a Student entitásra
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [ForeignKey("Course")] // Idegen kulcs a Course entitásra
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}
```

---

## 5. Az `AppDbContext` és a Fluent API beállítása

Hozz létre egy `AppDbContext.cs` fájlt. A `DbContext` valósítja meg a **UnitOfWork** és **Repository** tervezési mintákat.

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<StudentCourse> StudentCourses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Összetett elsődleges kulcs beállítása a kapcsolótáblához
        modelBuilder.Entity<StudentCourse>()
            .HasKey(sc => new { sc.StudentId, sc.CourseId });

        // N:M Kapcsolatok konfigurálása Fluent API-val
        modelBuilder.Entity<StudentCourse>()
            .HasOne(sc => sc.Student)
            .WithMany(s => s.StudentCourses)
            .HasForeignKey(sc => sc.StudentId);

        modelBuilder.Entity<StudentCourse>()
            .HasOne(sc => sc.Course)
            .WithMany(c => c.CourseCourses)
            .HasForeignKey(sc => sc.CourseId);
    }
}
```

---

## 6. A `Program.cs` konfigurálása

A `Program.cs`-ben regisztrálni kell a `DbContext`-et a függőség-befecskendezőben (Dependency Injection). **Fontos, hogy az `AddDbContext` a `builder.Build()` sora ELŐTT szerepeljen!**

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Kapcsolati string kiolvasása
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. DbContext regisztrálása (KÖTELEZŐEN a builder.Build() előtt!)
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(connStr));

var app = builder.Build();

app.MapGet("/", () => "Az EF Core alkalmazás fut!");

app.Run();
```

---

## 7. Tervezési idejű DbContext Factory (`AppDbContextFactory.cs`)

Annak érdekében, hogy a `dotnet ef` parancsok tervezési időben (design-time) garantáltan és hibamentesen példányosítani tudják a `DbContext`-et, hozz létre egy `AppDbContextFactory.cs` fájlt a projekt gyökerében:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Cseréld ki a saját adatbázis kapcsolati karakterláncodra:
        var connectionString = "Server=localhost;Database=MintaDb;User=root;Password=secret;";
        
        optionsBuilder.UseMySQL(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
```

---

## 8. Adatbázis-migrációk futtatása

Futtasd az alábbi parancsokat a projekt mappájában a parancssorban/terminalban:

1. **Migráció létrehozása:**
   ```bash
   dotnet ef migrations add InitialCreate
   ```
2. **Adatbázis és táblák létrehozása/frissítése:**
   ```bash
   dotnet ef database update
   ```

### Egyéb hasznos migrációs parancsok:
* **Legutóbbi migráció törlése:** `dotnet ef migrations remove`
* **Migrációk listázása:** `dotnet ef migrations list`
* **Erőltetett újragenerálás:** `dotnet ef migrations add InitialCreate --force`

---

## 9. Kipróbálás és Tesztelés (Minimal API Végpontok & LINQ)

Itt kipróbálhatod, hogyan fut le a C# LINQ lekérdezés, és miként alakul át SQL utasítássá. Egészítsd ki a `Program.cs` fájlodat az alábbi teszt végpontokkal a `builder.Build()` **után**:

```csharp
// --- TESZT VÉGPONT 1: Adatbeszúrás és LINQ Lekérdezés ---
app.MapGet("/diak-teszt", (AppDbContext context) =>
{
    // 1. Tesztadat hozzáadása, ha az adatbázis még üres
    if (!context.Students.Any())
    {
        context.Students.Add(new Student { FirstName = "xy" });
        context.SaveChanges();
    }

    // 2. LINQ lekérdezés futtatása
    var result = context.Students
                        .Where(s => s.FirstName == "xy")
                        .ToList();

    return result;
});

// --- TESZT VÉGPONT 2: Generált SQL Utasítás Megtekintése ---
app.MapGet("/sql-teszt", (AppDbContext context) =>
{
    // LINQ lekérdezés előkészítése
    var query = context.Students.Where(s => s.FirstName == "xy");

    // A lefordított SQL utasítás kinyerése szövegként (.ToQueryString())
    string generatedSql = query.ToQueryString();

    // Lekérdezés tényleges lefuttatása
    var data = query.ToList();

    return new 
    {
        GeneraltSql = generatedSql,
        Eredmeny = data
    };
});
```

### Futtatás és tesztelés:
1. Indítsd el az alkalmazást: `dotnet run` (vagy Visual Studio-ban **F5**).
2. Nyisd meg a böngészőben a kiírt címet a végpontokkal kiegészítve:
   * `https://localhost:<port>/diak-teszt`
   * `https://localhost:<port>/sql-teszt` (Itt láthatod a `SELECT ... WHERE KeresztNev = 'xy'` SQL parancsot).

---

## 10. EF Core Háttérműködés: Change Tracker és Tranzakciók

### A) Change Tracker (Változáskövető) állapotok
Lekérdezéskor az EF Core pillanatképet (snapshotot) készít az adatokról, majd a `SaveChanges()` hívásakor összehasonlítja azokat:
* **Detached:** Az entitás nincs a `DbContext` felügyelete alatt.
* **Unchanged:** Beolvasva az adatbázisból, semmi nem változott rajta.
* **Added:** Új entitás (`context.Add()`), mentéskor **`INSERT`** utasítás készül belőle.
* **Modified:** Legalább egy mezője megváltozott, mentéskor **`UPDATE`** készül belőle.
* **Deleted:** Törlésre kijelölve, mentéskor **`DELETE`** készül belőle.

### B) Tranzakciókezelés
* **Implicit tranzakció:** A `context.SaveChanges()` automatikusan egyetlen tranzakcióba fogja a műveleteket. Hiba esetén automatikus rollback történik.
* **Explicit tranzakció (Kézi vezérlés):**
  ```csharp
  using var transaction = await context.Database.BeginTransactionAsync();
  try
  {
      context.Students.Add(new Student { FirstName = "Anna" });
      await context.SaveChangesAsync();

      await transaction.CommitAsync();
  }
  catch (Exception)
  {
      await transaction.RollbackAsync();
  }
  ```