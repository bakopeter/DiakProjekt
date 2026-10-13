using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; } = null!;

    public List<StudentCourse> CourseCourses { get; set; } = new();
}