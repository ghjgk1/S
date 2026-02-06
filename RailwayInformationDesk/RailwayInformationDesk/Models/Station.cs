using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class Station
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Region { get; set; } = null!;

    public string City { get; set; } = null!;

    public virtual ICollection<Route> RouteArrivalStations { get; set; } = new List<Route>();

    public virtual ICollection<Route> RouteDepartureStations { get; set; } = new List<Route>();

    public virtual ICollection<ScheduleTemplateStop> ScheduleTemplateStops { get; set; } = new List<ScheduleTemplateStop>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
