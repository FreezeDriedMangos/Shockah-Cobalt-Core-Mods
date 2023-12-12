﻿using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class Config
{
	public List<double> BotchChances = new() { 0.15, 0.14, 0.12, 0.10, 0.08, 0.06, 0.05 };
	public List<double> DoubleChances = new() { 0.05, 0.06, 0.08, 0.10, 0.12, 0.14, 0.15 };
	public double DualApologyChance = 0.25;
}
