using LightningRisk.Core;
using LightningRisk.WebApi.Entities;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LightningRisk.WebApi.Services;

public class SubscriptionService(ITelegramBotClient client, ISqlSugarClient db)
{
}