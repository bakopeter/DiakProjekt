using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Kapcsolótábla az N:M kapcsolathoz
public class StudentCourse
{
    [ForeignKey("Student")] // Idegen kulcs
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [ForeignKey("Course")] // Idegen kulcs
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}
