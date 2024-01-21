
namespace Shockah.Kokoro;

public class ATooltipDummy : ADummyAction
{
    public delegate void OnGetTooltips(State s);

    public OnGetTooltips onGetTooltips;
    public List<Tooltip> tooltips;
    public List<Icon>? icons;

    public static ATooltipDummy BuildStandIn(CardAction action, State s)
    {
        if (action is AAttack aattack)
        {
            return BuildFromAttack(aattack, s);
        }

        if (action is AStatus astatus)
        {
            return BuildFromStatus(astatus, s);
        }

        Icon? icon = action.GetIcon(s);
        return new ATooltipDummy()
        {
            tooltips = action.GetTooltips(s),
            icons = icon == null ? new() : new() { (Icon)action.GetIcon(s) }
        };
    }

    public static ATooltipDummy BuildFromStatus(AStatus astatus, State s)
    {
        List<Icon> icons = new();

        if (!astatus.targetPlayer)
        {
            icons.Add(new Icon(Enum.Parse<Spr>("icons_outgoing"), null, Colors.textMain));
        }

        Icon? icon = astatus.GetIcon(s);
        if (icon != null)
        {
            icons.Add((Icon)icon);
        }


        return new ATooltipDummy()
        {
            tooltips = astatus.GetTooltips(s),
            icons = icons
        };
    }

    public static List<Tooltip> GetTooltipsNoSideEffects(AAttack aattack, State s)
    {
        var sshipparts = s.ship.parts;
        s.ship.parts = new();
        var c = s.route is Combat combat ? combat : null;
        var cstuff = c != null ? c.stuff : null;
        if (c != null) c.stuff = new();

        var tooltips = aattack.GetTooltips(s);

        if (c != null) c.stuff = cstuff;
        s.ship.parts = sshipparts;

        return tooltips;
    }

    public static ATooltipDummy BuildFromAttack(AAttack aattack, State s, bool hideOutgoingArrow = true)
    {
        List<Icon> icons = new();
        var tooltips = GetTooltipsNoSideEffects(aattack, s);

        icons.Add(new Icon(Enum.Parse<Spr>("icons_attack"), aattack.damage, Colors.redd));

        if (aattack.stunEnemy)
        {
            icons.Add(new Icon(Enum.Parse<Spr>("icons_stun"), null, Colors.textMain));
        }

        if (aattack.status != null)
        {
            // this cast is ridiculous. It's needed because C# still thinks aattack.status can be null, even though it must be non-null to get here
            var icon = new AStatus()
            {
                status = (Status)aattack.status,
                targetPlayer = hideOutgoingArrow,
                statusAmount = aattack.statusAmount
            }.GetIcon(s);

            if (icon != null)
            {
                if (!hideOutgoingArrow) icons.Add(new Icon(Enum.Parse<Spr>("icons_outgoing"), null, Colors.textMain));
                icons.Add((Icon)icon);
            }
        }

        if (aattack.moveEnemy != 0)
        {
            Spr spr = aattack.moveEnemy < 0
                ? Enum.Parse<Spr>("icons_moveLeftEnemy")
                : Enum.Parse<Spr>("icons_moveRightEnemy");
            icons.Add(new Icon(spr, Math.Abs(aattack.moveEnemy), Colors.redd));
        }

        return new ATooltipDummy()
        {
            tooltips = tooltips,
            icons = icons,
            onGetTooltips = (s) => aattack.GetTooltips(s)
        };
    }

    public override List<Tooltip> GetTooltips(State s)
    {
        if (onGetTooltips != null) onGetTooltips(s);
        return tooltips ?? new();
    }

    public override Icon? GetIcon(State s)
    {
        return null;
    }

    private static void IconAndOrNumber(Spr icon, ref bool isFirst, ref int w, G g, CardAction action, State state, Color spriteColor, bool dontDraw, int iconNumberPadding = 2, int iconWidth = 8, int numberWidth = 6, int? amount = null, Color? textColor = null, bool flipY = false, int? x = null, int iconPadding = 4)
    {
        if (!isFirst)
        {
            w += iconPadding;
        }
        if (!dontDraw)
        {
            Rect? rect = new Rect(w);
            Vec xy = g.Push(null, rect).rect.xy;
            Draw.Sprite(icon, xy.x, xy.y, flipX: false, flipY, 0.0, null, null, null, null, spriteColor);
            g.Pop();
        }
        w += iconWidth;
        if (amount.HasValue)
        {
            int valueOrDefault4 = amount.GetValueOrDefault();
            if (!x.HasValue)
            {
                w += iconNumberPadding;
                string text = DB.IntStringCache(valueOrDefault4);
                if (!dontDraw)
                {
                    Rect? rect = new Rect(w);
                    Vec xy = g.Push(null, rect).rect.xy;
                    BigNumbers.Render(valueOrDefault4, xy.x, xy.y, textColor ?? Colors.textMain);
                    g.Pop();
                }
                w += text.Length * numberWidth;
            }
        }
        if (x.HasValue)
        {
            if (x < 0)
            {
                w += iconNumberPadding;
                if (!dontDraw)
                {
                    Rect? rect = new Rect(w - 2);
                    Vec xy = g.Push(null, rect).rect.xy;
                    Color? color14 = (action.disabled ? new Color?(spriteColor) : textColor);
                    Draw.Sprite(Enum.Parse<Spr>("icons_minus"), xy.x, xy.y - 1.0, flipX: false, flipY: false, 0.0, null, null, null, null, color14);
                    g.Pop();
                }
                w += 3;
            }
            if (Math.Abs(x.Value) > 1)
            {
                w += iconNumberPadding + 1;
                if (!dontDraw)
                {
                    G g18 = g;
                    Rect? rect12 = new Rect(w);
                    Vec xy16 = g18.Push(null, rect12).rect.xy;
                    BigNumbers.Render(Math.Abs(x.Value), xy16.x, xy16.y, textColor ?? Colors.textMain);
                    g.Pop();
                }
                w += 4;
            }
            w += iconNumberPadding;
            if (!dontDraw)
            {
                G g19 = g;
                Rect? rect12 = new Rect(w);
                Vec xy17 = g19.Push(null, rect12).rect.xy;
                Spr? id13 = Enum.Parse<Spr>("icons_x_white");
                double x17 = xy17.x;
                double y16 = xy17.y - 1.0;
                Color? color14 = action.GetIcon(state)?.color;
                Draw.Sprite(id13, x17, y16, flipX: false, flipY: false, 0.0, null, null, null, null, color14);
                g.Pop();
            }
            w += 8;
        }
        isFirst = false;
    }
}