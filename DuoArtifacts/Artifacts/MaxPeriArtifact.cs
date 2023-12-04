﻿using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class MaxPeriArtifact : DuoArtifact
{
	private static int ModifyBaseDamageNestingCounter = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(GetType(), nameof(Card_GetActionsOverridden_Postfix))
		);
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);

		var oldHand = combat.hand.ToList();
		oldHand.Insert(handPosition, card);
		var attacks = oldHand.Where(c => c == card || c.GetActions(state, combat).Any(a => a is AAttack)).ToList();
		if (!attacks.Contains(card) || attacks.Count == 1)
			return;

		Pulse();
	}

	public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
	{
		if (!fromPlayer || card is null || combat is null)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		if (ModifyBaseDamageNestingCounter > 0)
			return 0;
		ModifyBaseDamageNestingCounter++;
		var attacks = combat.hand.Where(c => c == card || c.GetActions(state, combat).Any(a => a is AAttack)).ToList();
		ModifyBaseDamageNestingCounter--;

		if (!attacks.Contains(card) || attacks.Count == 1)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		if (attacks.First() != card)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		return 1;
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		if (!__result.Any(a => a is AAttack))
			return;

		var artifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is MaxPeriArtifact);
		if (artifact is null)
			return;

		var attacks = c.hand.Where(card => card == __instance || card.GetActions(s, c).Any(a => a is AAttack)).ToList();
		if (!attacks.Contains(__instance) || attacks.Count == 1)
			return;

		if (attacks.Last() != __instance)
			return;

		__result.Add(new AAttack { damage = __instance.GetDmg(s, 1) });
	}
}