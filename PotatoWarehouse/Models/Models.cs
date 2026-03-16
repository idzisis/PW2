using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PotatoWarehouse.Models;

public class Season
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public double TargetWeight { get; set; }

    public virtual ICollection<Variety> Varieties { get; set; } = new List<Variety>();
    public virtual ICollection<Caliber> Calibers { get; set; } = new List<Caliber>();
    public virtual ICollection<IncomingPotato> IncomingPotatoes { get; set; } = new List<IncomingPotato>();
    public virtual ICollection<OutgoingPotato> OutgoingPotatoes { get; set; } = new List<OutgoingPotato>();
}

public class Variety
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SeasonId { get; set; }

    [ForeignKey("SeasonId")]
    public virtual Season? Season { get; set; }
}

public class Caliber
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public int SeasonId { get; set; }

    [ForeignKey("SeasonId")]
    public virtual Season? Season { get; set; }
}

public class IncomingPotato
{
    [Key]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int VarietyId { get; set; }

    [ForeignKey("VarietyId")]
    public virtual Variety? Variety { get; set; }

    public int CaliberId { get; set; }

    [ForeignKey("CaliberId")]
    public virtual Caliber? Caliber { get; set; }

    public double ContainerWeight { get; set; }

    public int ContainerCount { get; set; }

    public double TotalWeight => ContainerWeight * ContainerCount;
    
    public double TotalWeightTons => (ContainerWeight * ContainerCount) / 1000.0;

    public double ContainerWeightTons => ContainerWeight / 1000.0;

    public int SeasonId { get; set; }

    [ForeignKey("SeasonId")]
    public virtual Season? Season { get; set; }
}

public class OutgoingPotato
{
    [Key]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int VarietyId { get; set; }

    [ForeignKey("VarietyId")]
    public virtual Variety? Variety { get; set; }

    public int CaliberId { get; set; }

    [ForeignKey("CaliberId")]
    public virtual Caliber? Caliber { get; set; }

    public double ContainerWeight { get; set; }

    public int ContainerCount { get; set; }

    public double TotalWeight => ContainerWeight * ContainerCount;
    
    public double TotalWeightTons => (ContainerWeight * ContainerCount) / 1000.0;

    public double ContainerWeightTons => ContainerWeight / 1000.0;

    [Required]
    [MaxLength(200)]
    public string Buyer { get; set; } = string.Empty;

    public int SeasonId { get; set; }

    [ForeignKey("SeasonId")]
    public virtual Season? Season { get; set; }

    public int? IncomingId { get; set; }

    [ForeignKey("IncomingId")]
    public virtual IncomingPotato? Incoming { get; set; }
}

public class AppSettings
{
    [Key]
    public int Id { get; set; }

    public int? ActiveSeasonId { get; set; }
}
