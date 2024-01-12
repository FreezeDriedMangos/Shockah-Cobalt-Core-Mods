﻿using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SummonBatCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("SummonBat", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SummonBat", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 3 : 1
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ASpawn
			{
				thing = new BatStuff
				{
					targetPlayer = false,
					yAnimation = 1
				}
			}
		];
}