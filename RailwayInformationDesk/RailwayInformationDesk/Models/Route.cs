using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class Route
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int DepartureStationId { get; set; }

    public int ArrivalStationId { get; set; }

    public virtual Station ArrivalStation { get; set; } = null!;

    public virtual Station DepartureStation { get; set; } = null!;

    public virtual ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
}
