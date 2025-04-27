using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace LightningRisk.WebApi.Entities;

public class Subscription
{
    [SugarColumn(IsPrimaryKey = true)] 
    public long ChatId { get; set; }

    [MaxLength(3)]
    [SugarColumn(IsPrimaryKey = true)]
    public string SectorCode { get; set; }
}