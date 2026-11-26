using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Queued operations, colored by whether the next can start (green), is paused (yellow) or is stuck (red), with an add button
    [StaticConstructorOnStartup]
    public class OperationsColumn : SummaryColumn
    {
        private const float AddSize = 24f;
        private const float DotSize = 8f;

        private static readonly Texture2D AddTex = CoreWidgets.Icon("plus");

        // Session lifetime, from translations only
        private static string? addTitle;
        private static string? addTip;

        public override bool HeadingLeft => true;

        public OperationsColumn()
        {
            width = 230f;
        }

        private static bool CanOperate(Pawn pawn) => pawn.Spawned && pawn.RaceProps.IsFlesh && pawn.BillStack != null;

        public override void Cell(Rect rect, PawnSummary row)
        {
            Pawn pawn = row.pawn;
            OperationStatus ops = row.Health.operations;
            float right = rect.xMax - 4f;
            if (CanOperate(pawn))
            {
                addTitle ??= "ColonistSummary_AddOperation".Translate();
                addTip ??= "ColonistSummary_AddOperationTip".Translate();
                Rect add = new Rect(right - AddSize, Mathf.Round(rect.center.y - AddSize / 2f), AddSize, AddSize);
                if (CoreWidgets.HeaderButton(add, AddTex, Theme.HighlightGreen, addTitle, addTip, 14f))
                {
                    OperationsMenu.Open(pawn);
                }
                right = add.x - 4f;
            }
            if (ops.count == 0) return;
            Rect text = new Rect(rect.x + 6f, rect.y, right - rect.x - 6f, rect.height);
            Rect dot = new Rect(text.x, Mathf.Round(rect.center.y - DotSize / 2f), DotSize, DotSize);
            Rounded.Fill(dot, DangerLook.Accent(ops.danger));
            text.xMin = dot.xMax + 6f;
            CoreWidgets.Line(text, ops.text, Theme.TextBright, GameFont.Small, TextAnchor.MiddleLeft);
            CoreWidgets.Tip(new Rect(rect.x, rect.y, right - rect.x, rect.height), label, ops.tip, null, DangerLook.Accent(ops.danger));
        }

        // Stuck operations first when sorted descending, then paused, then the rest by how many are queued
        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static int Value(PawnSummary row)
        {
            OperationStatus ops = row.Health.operations;
            return ops.count == 0 ? 0 : (ops.danger == Danger.Losing ? 3000 : ops.danger == Danger.Close ? 2000 : 1000) + ops.count;
        }
    }
}
