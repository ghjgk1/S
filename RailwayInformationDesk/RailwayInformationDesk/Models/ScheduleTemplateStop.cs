using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class ScheduleTemplateStop
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public int StationId { get; set; }

    public int StopOrder { get; set; }

    public TimeOnly? ArrivalTime { get; set; }

    public TimeOnly? DepartureTime { get; set; }

    public string? Platform { get; set; }

    public virtual ICollection<ActualSchedule> ActualSchedules { get; set; } = new List<ActualSchedule>();

    public virtual Station Station { get; set; } = null!;

    public virtual ScheduleTemplate Template { get; set; } = null!;
}
