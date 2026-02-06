using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class ActualSchedule
{
    public int Id { get; set; }

    public int TripInstanceId { get; set; }

    public int StopId { get; set; }

    public DateTime? ActualArrivalTime { get; set; }

    public DateTime? ActualDepartureTime { get; set; }

    public string? ActualPlatform { get; set; }

    public int? DelayMinutes { get; set; }

    public virtual ScheduleTemplateStop Stop { get; set; } = null!;

    public virtual TripInstance TripInstance { get; set; } = null!;
}
