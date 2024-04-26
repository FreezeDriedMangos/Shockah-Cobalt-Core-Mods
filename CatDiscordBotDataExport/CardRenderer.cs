using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class RendererHelper
{
	private static RenderTarget2D? CurrentRenderTarget;

	internal delegate void RenderDelegate();

	internal static void RenderToPng(G g, Stream stream, Vec imageSize, bool withScreenFilter, string identifier, RenderDelegate renderLogic)
	{
		if (CurrentRenderTarget is null || CurrentRenderTarget.Width != (int)(imageSize.x * g.mg.PIX_SCALE) || CurrentRenderTarget.Height != (int)(imageSize.y * g.mg.PIX_SCALE))
		{
			CurrentRenderTarget?.Dispose();
			CurrentRenderTarget = new(g.mg.GraphicsDevice, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
		}

		var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

		g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

		g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
		Draw.StartAutoBatchFrame();
		try
		{
			renderLogic();
		}
		catch
		{
			ModEntry.Instance.Logger.LogError("There was an error exporting {identifier}.", identifier);
		}
		if (withScreenFilter)
			Draw.Rect(0, 0, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE), Colors.screenOverlay, new BlendState
			{
				ColorBlendFunction = BlendFunction.Add,
				ColorSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.InverseSourceColor,
				AlphaSourceBlend = Blend.DestinationAlpha,
				AlphaDestinationBlend = Blend.DestinationAlpha
			});
		Draw.EndAutoBatchFrame();

		g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

		CurrentRenderTarget.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
	}
}

internal sealed class CardRenderer
{
	internal static Dictionary<Deck, List<Tooltip>> CardPosterTooltips = new();
	internal static Texture2D? CardOutlineSprite;

	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);
	private static readonly int HorizontalSpacing = 8;
	private static readonly int VerticalSpacing = 8;

	// todo: replace HashSet starters with Dictionary<Type, Color> backgrounds
	public void RenderCollection(G g, bool withScreenFilter, List<List<Card>> rows, Stream stream, Dictionary<Type, Color>? backgrounds = null, List<Tooltip>? tooltips = null)
	{
		int maxWidth = 0;
		int maxHeight = 0;
		int maxRowSize = 0;
		foreach (var row in rows)
		{
			maxRowSize = Math.Max(maxRowSize, row.Count);
			foreach (var card in row)
			{
				var cardSize = GetImageSize(card);
				maxWidth = (int)Math.Max(maxWidth, cardSize.x);
				maxHeight = (int)Math.Max(maxHeight, cardSize.y);
			}
		}

		Vec imageSize = new(maxRowSize*maxWidth + (maxRowSize)*HorizontalSpacing, rows.Count*maxHeight + (rows.Count)*VerticalSpacing);

		RendererHelper.RenderToPng(g, stream, imageSize, withScreenFilter, $"Deck {rows.First().First().GetMeta().deck.Key()}", () =>
		{
			int curX = HorizontalSpacing / 2;
			int curY = VerticalSpacing / 2;
			foreach (var row in rows)
			{
				foreach (var card in row)
				{
					try
					{
						if (backgrounds?.ContainsKey(card.GetType()) ?? false)
						{
							Draw.Sprite(GetCardOutlineSprite(), curX - 5, curY - 5, color: backgrounds[card.GetType()]);
						}
						
						card.Render(g, posOverride: new Vec(curX, curY), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
					}
					catch
					{
						ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
					}

					curX += maxWidth + HorizontalSpacing;
				}

				curX = HorizontalSpacing / 2;
				curY += maxHeight + VerticalSpacing;
			}
		});
	}

	public void Render(G g, bool withScreenFilter, Card card, Stream stream)
	{
		var imageSize = GetImageSize(card);
		RendererHelper.RenderToPng(g, stream, imageSize, withScreenFilter, $"Card {card.Key()}", () =>
		{
			card.Render(g, posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
		});
	}

	private Vec GetImageSize(Card card)
	{
		var meta = card.GetMeta();
		if (meta.deck is Deck.corrupted or Deck.evilriggs)
			return OverborderCardSize;

		if (ModEntry.Instance.Helper.Content.Decks.LookupByDeck(meta.deck) is { } deckEntry && deckEntry.Configuration.OverBordersSprite is { } overBordersSprite)
		{
			var texture = SpriteLoader.Get(overBordersSprite);
			if (texture is not null)
				return new(texture.Width, texture.Height);
		}
		return BaseCardSize;
	}

	private Texture2D GetCardOutlineSprite()
	{
		if (CardOutlineSprite != null) return CardOutlineSprite;

		using Stream fs = ModEntry.Instance.Package.PackageRoot.GetRelativeFile("cardOutline.png").OpenRead();
		CardOutlineSprite = Texture2D.FromStream(MG.inst.GraphicsDevice, fs);
		return CardOutlineSprite;
	}
}

internal class TooltipRenderer
{
	private void Render(G g, bool withScreenFilter, Tooltip tooltip, Stream stream)
	{
		var imageBounds = tooltip.Render(g, dontDraw: true);
		var imageSize = new Vec(imageBounds.w, imageBounds.h);
		RendererHelper.RenderToPng(g, stream, imageSize, withScreenFilter, $"Tooltip {tooltip.GetType().FullName}", () =>
		{
			tooltip.Render(g, false);
		});
	}
}

internal class SkylineBottomLeft
{
	class SkylineFragment
	{
		public int width;
		public int maxWidthAllowingOverhangs;
		public int elevation;
	}

	public class IntVec
	{
		public int x;
		public int y;
	}

	List<SkylineFragment> skyline = new();
	List<SkylineFragment> skylineByElevation = new();

	public SkylineBottomLeft(int width)
	{
		skyline.Add(new() { width=width, elevation=0, maxWidthAllowingOverhangs=width });
	}

	public void InsertRectAt(IntVec xy, IntVec size)
	{
		int elevation = xy.y + size.y;

		int x = 0;
		for (var i = 0; i < skyline.Count; i++)
		{
			if (x + skyline[i].width > xy.x)
			{
				// put rectangle here
				var originalWidth = skyline[i].width;
				var preceedingWidth = xy.x - x;
				var postceedingWidth = (skyline[i].width - preceedingWidth) - size.x;
				skyline[i].width = preceedingWidth;
				skyline.Insert(i+1, new()
				{
					width = size.x,
					elevation = xy.y+size.y
				});
				if (postceedingWidth > 0)
				{
					skyline.Insert(i+2, new()
					{
						width = postceedingWidth,
						elevation = skyline[i].elevation
					});
				}
				else
				{
					int w = Math.Abs(postceedingWidth);
					for (var j = i+2; w > 0 && j < skyline.Count; j++)
					{
						skyline[j].width = Math.Max(skyline[j].width, w);

						w -= skyline[j].width;
					}
				}

				break;
			}
		}

		RecalculateAvailableSkylineFrom(skyline.Count-1);
	}

	public IntVec AllocateRect(int w, int h)
	{
		// 
		// XX
		// XX
		// XXOOOO
		// XXOOOO   II
		// XXOOOO+++II
		// 
		// inserting
		// HHHHH
		// HHHHH
		// 
		// 
		// result
		// XXHHHHH
		// XXHHHHH
		// XXOOOO.
		// XXOOOO.  II
		// XXOOOO+++II
		// 
		// 

		// step 1: iterate over skyline, lowest to highest elevation
		// keep track of current max width as so:

		int x = 0;
		for(var i = 0; i < skyline.Count; i++) 
		{
			var fragment = skyline[i];

			if (fragment.maxWidthAllowingOverhangs >= w)
			{
				int y = fragment.elevation;

				if (w == fragment.width)
				{
					fragment.elevation += h;
					RecalculateAvailableSkylineFrom(i);
				}
				else if (w < fragment.width)
				{
					skyline[i].width -= w;
					skyline[i].maxWidthAllowingOverhangs -= w;

					skyline.Insert(i, new()
					{
						width = w,
						elevation = fragment.elevation + (int)h
					});

					RecalculateAvailableSkylineFrom(i);
				}
				else
				{
					var wLeft = w;
					int j = i;
					for (; j < fragment.width && wLeft > 0; j++)
					{
						// if there's an index out of bounds exception here, that means that
						// fragment.maxWidthAllowingOverhangs is inaccurate 

						skyline[j].width -= Math.Min(skyline[j].width, wLeft);
						skyline[j].maxWidthAllowingOverhangs -= Math.Min(skyline[j].width, wLeft);
						wLeft -= skyline[j].width;

						if (skyline[j].width <= 0)
						{
							skyline.RemoveAt(j);
							j--;
						}
					}

					fragment.elevation += h;
					fragment.width = w;
					RecalculateAvailableSkylineFrom(j);
				}

				return new IntVec()
				{
					x = x,
					y = y,
				};
			} 
			else
			{
				x += fragment.width;
			}
		}

		throw new Exception("Rectangle does not fit");
	}

	// recalculates all possibly affected fragments when the fragment at fragmentIdx changes elevation/width
	private void RecalculateAvailableSkylineFrom(int fragmentIdx)
	{
		if (fragmentIdx == skyline.Count - 1)
		{
			skyline[skyline.Count - 1].maxWidthAllowingOverhangs = skyline[skyline.Count - 1].width;
			fragmentIdx--;
		}

		for (var i = fragmentIdx; i >= 0; i--)
		{
			if (skyline[i].elevation < skyline[i + 1].elevation) skyline[i].maxWidthAllowingOverhangs = skyline[i].width;
			else skyline[i].maxWidthAllowingOverhangs = skyline[i].width + skyline[i + 1].maxWidthAllowingOverhangs;
		}
	}
}