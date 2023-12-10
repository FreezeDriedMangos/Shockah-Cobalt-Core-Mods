﻿using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

[ArtifactMeta(unremovable = true)]
internal sealed class SmugnessArtifact : Artifact
{
	public enum SmugResult
	{
		Botch, Normal, Double
	}

	private static ModEntry Instance => ModEntry.Instance;

	private static readonly double[] BotchChances = new double[] { 0.15, 0.14, 0.12, 0.10, 0.08, 0.06, 0.05, 1.00 };
	private static readonly double[] DoubleChances = new double[] { 0.05, 0.06, 0.08, 0.10, 0.12, 0.14, 0.15, 0.00 };
	private static readonly Type[] ApologyCards = new Type[] { typeof(AttackApologyCard), typeof(ShieldApologyCard) };

	private static bool IsDuringTryPlayCard = false;
	private static bool HasPlayNoMatterWhatForFreeSet = false;

	public int Smugness = (BotchChances.Length - 1) / 2;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(typeof(SmugnessArtifact), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(typeof(SmugnessArtifact), nameof(Combat_TryPlayCard_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(typeof(SmugnessArtifact), nameof(Card_GetActionsOverridden_Postfix))
		);
	}

	public SmugResult GetSmugResult(Rand rng)
	{
		double botchChance = BotchChances[Smugness];
		double doubleChance = DoubleChances[Smugness];

		var result = rng.Next();
		if (result < botchChance)
			return SmugResult.Botch;
		else if (result < botchChance + doubleChance)
			return SmugResult.Double;
		else
			return SmugResult.Normal;
	}

	public void AddSmugness(int value)
		=> Smugness = Math.Clamp(Smugness + value, 0, BotchChances.Length - 1);

	public override int? GetDisplayNumber(State s)
		=> Smugness;

	private static void Combat_TryPlayCard_Prefix(bool playNoMatterWhatForFree)
	{
		IsDuringTryPlayCard = true;
		HasPlayNoMatterWhatForFreeSet = playNoMatterWhatForFree;
	}

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		if (!IsDuringTryPlayCard)
			return;
		if (HasPlayNoMatterWhatForFreeSet)
			return;

		var artifact = s.EnumerateAllArtifacts().OfType<SmugnessArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		var hook = Instance.FrogproofManager.GetHandlingHook(s, s.route as Combat, __instance, FrogproofHookContext.Action);
		if (hook is not null)
		{
			hook.PayForFrogproof(s, s.route as Combat, __instance);
			return;
		}

		var result = artifact.GetSmugResult(s.rngActions);
		switch (result)
		{
			case SmugResult.Botch:
				artifact.Pulse();
				__result.Clear();
				for (int i = 0; i < __instance.GetCurrentCost(s); i++)
					__result.Add(new AAddCard
					{
						card = (Card)Activator.CreateInstance(ApologyCards[s.rngActions.NextInt() % ApologyCards.Length])!,
						destination = CardDestination.Hand
					});

				if (artifact.Smugness == BotchChances.Length - 1)
					artifact.Smugness = 0;
				else
					artifact.AddSmugness(-1);
				break;
			case SmugResult.Double:
				artifact.Pulse();
				__result.AddRange(__result.ToList());
				artifact.AddSmugness(1);
				break;
		}
	}
}
