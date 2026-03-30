using System.ComponentModel.DataAnnotations;

public class EventLectureViewModel
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }
    public string? EventName { get; set; }

    [Required]
    public int LecturerId { get; set; }
    public string? LecturerName { get; set; }

    [Required]
    public DateTime DateTime { get; set; }

    [Required]
    public decimal DurationInHours { get; set; }
}