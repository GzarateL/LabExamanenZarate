using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Lab8_GaelZarate.Models;

public partial class Order
{
    [Key]
    public int OrderId { get; set; }

    public int ClientId { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime OrderDate { get; set; }

    [ForeignKey("ClientId")]
    [InverseProperty("Orders")]
    public virtual Client Client { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
