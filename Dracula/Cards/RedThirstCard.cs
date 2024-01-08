﻿using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class RedThirstCard : Card, IDraculaCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("RedThirst", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RedThirst", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			exhaust = true,
			retain = upgrade == Upgrade.A,
			buoyant = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				}
			],
			_ => [
				new AEnergy
				{
					changeAmount = 2
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				}
			]
		};
}
