using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Diakok")] // Táblanév felülírása
public class Student
{
    [Key] // Elsődleges kulcs
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Adatbázis által generált érték
    public int StudentId { get; set; }

    [Required] // NOT NULL megszorítás az adatbázisban
    [MaxLength(50)] // Karakterlánc maximális hossza
    [Column("KeresztNev", TypeName = "varchar(50)")] // SQL oszlopnév és típus testreszabása
    public string FirstName { get; set; } = null!;

    [NotMapped] // Nem kerül be az adatbázisba
    public string FullName => $"{FirstName}";

    // N:M kapcsolathoz tartozó kapcsolótábla listája
    public List<StudentCourse> StudentCourses { get; set; } = new();
}
