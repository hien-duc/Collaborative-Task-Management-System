using System.ComponentModel.DataAnnotations;

namespace Collaborative_Task_Management_System.Attributes;

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is DateTime date)
        {
            return date.Date >= DateTime.Today;
        }
        return false;
    }
}