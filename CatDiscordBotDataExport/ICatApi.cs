using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.CatDiscordBotDataExport
{
	public interface ICatApi
	{
		void RegisterTooltips(Deck deck, List<Tooltip> tooltips);
		List<Tooltip> GetTooltips(Deck deck);
	}
}
