using System.ComponentModel.DataAnnotations;
using Collaborative_Task_Management_System.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Collaborative_Task_Management_System.Models.ViewModels;

public class ProjectCreateViewModel
{
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", 
        MinimumLength = 3)]
    public string Title { get; set; }

    [StringLength(1000, ErrorMessage = "The {0} must be at most {1} characters long.")]
    public string Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Deadline")]
    [FutureDate(ErrorMessage = "Deadline must be a future date")]
    public DateTime Deadline { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    [Display(Name = "Priority")]
    public string Priority { get; set; } = "Medium";

    [Display(Name = "Team Members")]
    public List<string> SelectedTeamMemberIds { get; set; } = new List<string>();

    public List<SelectListItem> AvailableUsers { get; set; } = new List<SelectListItem>();
}