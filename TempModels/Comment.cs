using System;
using System.Collections.Generic;

namespace Collaborative_Task_Management_System.TempModels;

public partial class Comment
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int TaskId { get; set; }

    public string UserId { get; set; } = null!;
}
