using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Lab8_GaelZarate.Models;

public partial class Client
{
    [Key]
    public int ClientId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [InverseProperty("Client")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
