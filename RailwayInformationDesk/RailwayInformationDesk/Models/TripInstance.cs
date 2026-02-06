using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class TripInstance
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public DateOnly TripDate { get; set; }

    public virtual ICollection<ActualSchedule> ActualSchedules { get; set; } = new List<ActualSchedule>();

    public virtual ScheduleTemplate Template { get; set; } = null!;
}
