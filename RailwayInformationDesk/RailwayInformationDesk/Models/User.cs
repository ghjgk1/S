using System;
using System.Collections.Generic;

namespace RailwayInformationDesk.Models;

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public int? StationId { get; set; }

    public string FullName { get; set; } = null!;

    public virtual Station? Station { get; set; }
}
