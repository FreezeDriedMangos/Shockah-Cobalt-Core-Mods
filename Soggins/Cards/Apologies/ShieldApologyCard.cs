﻿using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class ShieldApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Shield",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Shield;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = Status.shield,
				statusAmount = 1,
				targetPlayer = true
			}
		};
}