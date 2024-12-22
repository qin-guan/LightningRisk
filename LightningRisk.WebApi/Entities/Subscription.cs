using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LightningRisk.WebApi.Entities;

[Index(nameof(ChatId), nameof(SectorCode), IsUnique = true)]
public class Subscription
{
    public int Id { get; set; }

    public required long ChatId { get; set; }
    [MaxLength(3)] public required string SectorCode { get; set; }
}