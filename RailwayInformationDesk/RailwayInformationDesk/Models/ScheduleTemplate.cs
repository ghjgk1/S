using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class ScheduleTemplate
{
    public int Id { get; set; }

    public int RouteId { get; set; }

    public int TrainTypeId { get; set; }

    public bool IsActive { get; set; }

    public string? DaysOfWeek { get; set; }

    public virtual Route Route { get; set; } = null!;

    public virtual ICollection<ScheduleTemplateStop> ScheduleTemplateStops { get; set; } = new List<ScheduleTemplateStop>();

    public virtual TrainType TrainType { get; set; } = null!;

    public virtual ICollection<TripInstance> TripInstances { get; set; } = new List<TripInstance>();
}
