﻿namespace Shockah.Soggins;

internal static class I18n
{
	public static string SogginsName => "Soggins";
	public static string SogginsDescription => "<c=B79CE5>SOGGINS</c>\nThis is that frog who keeps making mistakes. Why did we let him on the ship?";

	public static string SmugArtifactName => "Smug";
	public static string SmugArtifactDescription => "Start each combat with <c=status>SMUG</c>.";

	public static string VideoWillArtifactName => "Video Will";
	public static string VideoWillArtifactDescription => "Start each combat with 3 <c=status>FROGPROOFING</c>.";

	public static string RepeatedMistakesArtifactName => "Repeated Mistakes";
	public static string RepeatedMistakesArtifactDescription => "Start each combat with 4 <c=status>MISSILE MALFUNCTION</c>. At the end of each turn, <c=action>LAUNCH</c> a <c=midrow>SEEKER</c>.";

	public static string FrogproofCardTraitName => $"Frogproof";
	public static string FrogproofCardTraitText => $"This card ignores <c=status>SMUG</c>.";

	public static string SmugStatusName => "Smug";
	public static string SmugStatusDescription => "The current level of <c=B79CE5>Soggins'</c> smugness, which affects his chance to <c=cheevoGold>double</c> or <c=downside>botch</c> card effects.\nLow smugness will <c=downside>botch</c> more often, while high smugness will <c=cheevoGold>double</c> more often.\n<c=downside>Beware of reaching max smugness, as that WILL GUARANTEE a botch and set smugness to 0.</c>";
	public static string FrogproofingStatusName => "Frogproofing";
	public static string FrogproofingStatusDescription => "Whenever you play a card that is not <c=cardtrait>FROGPROOF</c>, temporarily give it <c=cardtrait>FROGPROOF</c> <c=downside>and decrease this by 1.</c>";
	public static string BotchesStatusName => "Botches";
	public static string BotchesStatusDescription => "The number of times an action has been <c=downside>botched</c> this combat through <c=status>SMUG</c>.";

	public static string ApologyCardName => "Halfhearted Apology";
	public static string BlankApologyCardText => $"*a random {ApologyCardName} card*";

	public static string SmugnessControlCardName => "Smugness Control";
	public static string PressingButtonsCardName => "Pressing Buttons";
	public static string TakeCoverCardName => "Take Cover!";
	public static string ZenCardName => "Zen";
	public static string ZenCardText => "Reset your <c=status>SMUG</c>.";
	public static string MysteriousAmmoCardName => "Mysterious Ammo";
	public static string RunningInCirclesCardName => "Running in Circles";
	public static string BetterSpaceMineCardName => "Better Space Mine";
	public static string ThoughtsAndPrayersCardName => "Thoughts and Prayers";
	public static string StopItCardName => "Stop It!";

	public static string HarnessingSmugnessCardName => "Harnessing Smugness";
	public static string SoSorryCardName => "So Sorry";
}
