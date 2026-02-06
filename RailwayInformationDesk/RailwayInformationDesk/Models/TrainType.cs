using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class TrainType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
}
