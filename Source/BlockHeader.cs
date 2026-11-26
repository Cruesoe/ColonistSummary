using System;
using System.Collections.Generic;
using System.Reflection;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Headings for runs of narrow columns (skills, capacities), drawn like UI Rethemed - Work's job headings in the style chosen there
    public static class BlockHeader
    {
        private const float LabelRow = 20f;
        private const float LabelFoot = 24f;
        private const float MaxRotatedLabel = 110f;
        private const float MinRotatedLabel = 40f;
        private const float IconSquare = 30f;
        private const int HeadSalt = 30414;

        private static readonly float SlantSin = Mathf.Sin(Mathf.PI / 3f);
        private static readonly float SlantCos = Mathf.Cos(Mathf.PI / 3f);
        private static readonly float SlantTan = Mathf.Tan(Mathf.PI / 3f);

        // Window lifetime: the style is read when the window opens, the sizes with each rebuild of the shown columns
        public static SkillHeaderStyle style;
        // Work's "Column outlines" setting: slanted headings get a line along each column edge
        private static bool outlines;
        private static float longestLabel;
        private static float lastOverhang;
        private static bool anyBlock;

        public static void Opened()
        {
            style = WorkHeaderStyle.Read();
            outlines = WorkHeaderStyle.Outlines();
            longestLabel = 0f;
            lastOverhang = 0f;
            anyBlock = false;
        }

        // Sizes the headings for the page's shown columns
        public static void Measure(List<SummaryColumn> columns)
        {
            longestLabel = 0f;
            lastOverhang = 0f;
            anyBlock = false;
            for (int i = 0; i < columns.Count; i++)
            {
                if (!(columns[i] is BlockColumn block)) continue;
                anyBlock = true;
                longestLabel = Mathf.Max(longestLabel, CoreWidgets.Width(block.label) + 6f);
                if (IsBlockEnd(columns, i))
                {
                    // How far a block's last classic label reaches past its column
                    float width = Mathf.Min(CoreWidgets.Width(block.label) + 4f, block.width * 2f - 4f);
                    lastOverhang = Mathf.Max(lastOverhang, Mathf.Ceil((width - block.width) / 2f));
                }
            }
        }

        public static bool IsBlockEnd(List<SummaryColumn> columns, int i) => columns[i] is BlockColumn && (i + 1 >= columns.Count || !(columns[i + 1] is BlockColumn));

        public static float Height
        {
            get
            {
                if (!anyBlock) return 54f;
                float rotated = Mathf.Clamp(longestLabel, MinRotatedLabel, MaxRotatedLabel);
                switch (style)
                {
                    case SkillHeaderStyle.Vertical: return Mathf.Round(rotated + LabelFoot + 6f);
                    case SkillHeaderStyle.Slanted: return Mathf.Round(rotated * SlantSin + LabelRow * SlantCos + LabelFoot);
                    case SkillHeaderStyle.Icons: return 56f;
                    default: return 54f;
                }
            }
        }

        // Space between a block and the next column, clear of the last classic label
        public static float TrailingGap => !anyBlock ? 0f : BlockGap + (style == SkillHeaderStyle.Classic ? lastOverhang : 0f);

        private const float BlockGap = 16f;

        // Returns true when clicked
        public static bool Draw(Rect cell, int index, BlockColumn column, bool sorted, bool descending)
        {
            Rect hit = new Rect(cell.x + 1f, 0f, cell.width - 2f, cell.height);
            Rect label = default;
            if (style == SkillHeaderStyle.Classic)
            {
                float width = Mathf.Min(CoreWidgets.Width(column.label) + 4f, cell.width * 2f - 4f);
                label = new Rect(Mathf.Round(cell.center.x - width / 2f), 3f + index % 2 * LabelRow, width, LabelRow);
                hit = new Rect(cell.x + 1f, label.y, cell.width - 2f, cell.height - label.y);
            }
            bool over;
            if (style == SkillHeaderStyle.Slanted)
            {
                // The slanted heading's area leans right with its label: the mouse counts when it is over this column's slanted band
                hit = new Rect(cell.x, 0f, cell.width + cell.height / SlantTan, cell.height);
                Vector2 mouse = Event.current.mousePosition;
                float along = mouse.x - (cell.yMax - mouse.y) / SlantTan;
                over = Mouse.IsOver(hit) && along >= cell.x && along < cell.xMax;
            }
            else
            {
                over = Mouse.IsOver(hit) || (style == SkillHeaderStyle.Classic && Mouse.IsOver(label));
            }
            over &= Event.current.mousePosition.x >= SummaryTable.NameWidth;
            float lit = ThemeMotion.Hover(cell, HeadSalt, over);

            if (lit > 0f && style == SkillHeaderStyle.Slanted)
            {
                SlantedBand(cell, new Color(1f, 1f, 1f, 0.06f * lit));
            }
            else if (lit > 0f && style != SkillHeaderStyle.Icons)
            {
                float top = style == SkillHeaderStyle.Classic ? label.yMax : 2f;
                Rounded.Fill(new Rect(cell.x + 2f, top, cell.width - 4f, cell.yMax - top - 2f), new Color(1f, 1f, 1f, 0.06f * lit));
            }

            Color color = sorted ? Color.white : Color.Lerp(Theme.TextDim, Color.white, lit);
            float foot = cell.yMax - LabelFoot;
            switch (style)
            {
                case SkillHeaderStyle.Vertical:
                    Rotated(cell, foot, column.label, color, -90f);
                    break;
                case SkillHeaderStyle.Slanted:
                    Rotated(cell, foot, column.label, color, -60f);
                    break;
                case SkillHeaderStyle.Icons:
                    IconHeading(cell, column, sorted, lit);
                    break;
                default:
                    CoreWidgets.Line(label, column.label, color, GameFont.Small, TextAnchor.MiddleCenter);
                    float lineBottom = cell.yMax - 12f;
                    if (lineBottom > label.yMax)
                    {
                        Theme.Fill(new Rect(Mathf.Round(cell.center.x), label.yMax, 1f, lineBottom - label.yMax), Faces.Fade(Color.white, sorted ? 0.5f : 0.15f + 0.25f * lit));
                    }
                    break;
            }
            if (sorted) CoreWidgets.SortMarker(new Rect(Mathf.Round(cell.center.x) - 4f, cell.yMax - 22f, 8f, 10f), descending);

            if (!over) return false;
            CoreWidgets.Tip(hit, column.label, column.tip);
            return Widgets.ButtonInvisible(hit, false);
        }

        private static void Rotated(Rect cell, float foot, string text, Color color, float angle)
        {
            Vector2 pivot;
            Rect rect;
            if (angle < -80f)
            {
                float length = Mathf.Max(10f, foot - 4f);
                pivot = new Vector2(Mathf.Round(cell.center.x), Mathf.Round(foot - length / 2f));
                rect = new Rect(pivot.x - length / 2f, pivot.y - LabelRow / 2f, length, LabelRow);
            }
            else
            {
                float baseY = foot - 2f;
                pivot = new Vector2(Mathf.Round(cell.center.x + (cell.yMax - baseY) / SlantTan), Mathf.Round(baseY));
                rect = new Rect(pivot.x, pivot.y - LabelRow / 2f, Mathf.Max(10f, (foot - 4f) / SlantSin), LabelRow);
            }
            Matrix4x4 matrix = GUI.matrix;
            try
            {
                Rotate(angle, pivot);
                CoreWidgets.Line(rect, text, color, GameFont.Small, TextAnchor.MiddleLeft);
            }
            finally
            {
                GUI.matrix = matrix;
            }
        }

        // Rotates about the pivot, then moves the pivot back where it was drawn: the 1.7 GUI backend's rotation drifts with
        // the window's position and the UI scale. Same correction as UI Rethemed - Work's headings; plain rotation without that backend
        private static void Rotate(float angle, Vector2 pivot)
        {
            bool before = DrawnAt(pivot, out Vector2 screen);
            UI.RotateAroundPivot(angle, pivot);
            if (before && DrawnAt(pivot, out Vector2 after))
            {
                Vector2 drift = screen - after;
                if (drift.sqrMagnitude >= 0.01f) GUI.matrix = Matrix4x4.TRS(new Vector3(drift.x, drift.y, 0f), Quaternion.identity, Vector3.one) * GUI.matrix;
            }
        }

        // Session lifetime: the GUI backend accessor, resolved once; the point converter is rebound when the backend changes
        private static readonly Func<object>? currentBackend = BindBackend();
        private static object? boundBackend;
        private static Func<Vector2, Vector2>? toDevice;

        private static Func<object>? BindBackend()
        {
            MethodInfo? getter = typeof(GUI).Assembly.GetType("UnityStubs.Gui.GuiBackend")?.GetProperty("Current", BindingFlags.Static | BindingFlags.Public)?.GetGetMethod();
            return getter == null ? null : (Func<object>?)Delegate.CreateDelegate(typeof(Func<object>), getter, false);
        }

        private static bool DrawnAt(Vector2 local, out Vector2 screen)
        {
            screen = default;
            object? backend = currentBackend?.Invoke();
            if (backend == null) return false;
            if (backend != boundBackend)
            {
                boundBackend = backend;
                MethodInfo? method = backend.GetType().GetMethod("GuiToDevicePoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Vector2) }, null);
                toDevice = method == null ? null : (Func<Vector2, Vector2>?)Delegate.CreateDelegate(typeof(Func<Vector2, Vector2>), backend, method, false);
            }
            if (toDevice == null) return false;
            screen = toDevice(local);
            return true;
        }

        public static bool DrawsLines => outlines && style == SkillHeaderStyle.Slanted;

        // How far a slanted line's top leans right of its foot
        public static float Slant(float height) => height / SlantTan;

        // A slanted line up the headings from the column edge at lx, as in Work
        public static void ColumnLine(float lx, float height)
        {
            float shift = height / SlantTan;
            Texture2D? edge = EdgeTexture(shift + 3f, height);
            if (edge == null) return;
            GUI.color = Faces.Fade(Theme.BGLight, 0.9f);
            GUI.DrawTexture(new Rect(lx - 1f, 0f, shift + 3f, height), edge);
            GUI.color = Color.white;
        }

        // Session lifetime: the line texture, rebuilt when the header size or UI scale changes
        private static Texture2D? edgeTex;
        private static int edgeWidth;
        private static int edgeHeight;

        // An anti-aliased one-pixel line from the bottom left to the top right at the slant angle
        private static Texture2D? EdgeTexture(float width, float height)
        {
            float scale = Prefs.UIScale;
            int w = Mathf.Max(2, Mathf.RoundToInt(width * scale));
            int h = Mathf.Max(2, Mathf.RoundToInt(height * scale));
            if (edgeTex != null && w == edgeWidth && h == edgeHeight) return edgeTex;
            if (edgeTex != null) UnityEngine.Object.Destroy(edgeTex);
            edgeWidth = w;
            edgeHeight = h;
            edgeTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float half = scale * 0.5f / SlantSin;
            float soft = 1f / SlantSin;
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float center = scale + (y + 0.5f) / SlantTan;
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((half - Mathf.Abs(x + 0.5f - center)) / soft + 0.5f));
                }
            }
            edgeTex.SetPixels(pixels);
            edgeTex.Apply(false, false);
            return edgeTex;
        }

        // Session lifetime: hover bands per size, at most eight kept
        private static readonly Dictionary<(int, int, int), Texture2D> bandTexes = new Dictionary<(int, int, int), Texture2D>();

        // Hover fill for a slanted heading: the column's band leaning right with the labels
        private static void SlantedBand(Rect cell, Color color)
        {
            Rect position = new Rect(Mathf.Round(cell.x), cell.y, Mathf.Round(cell.width + cell.height / SlantTan), cell.height);
            Texture2D? band = BandTexture(position.width, position.height, cell.width);
            if (band == null) return;
            GUI.color = color;
            GUI.DrawTexture(position, band);
            GUI.color = Color.white;
        }

        private static Texture2D? BandTexture(float width, float height, float column)
        {
            float scale = Prefs.UIScale;
            int w = Mathf.Max(2, Mathf.RoundToInt(width * scale));
            int h = Mathf.Max(2, Mathf.RoundToInt(height * scale));
            int c = Mathf.RoundToInt(column * scale);
            (int, int, int) key = (w, h, c);
            if (bandTexes.TryGetValue(key, out Texture2D? cached) && cached != null) return cached;
            if (bandTexes.Count >= 8)
            {
                foreach (Texture2D old in bandTexes.Values)
                {
                    if (old != null) UnityEngine.Object.Destroy(old);
                }
                bandTexes.Clear();
            }
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            bandTexes[key] = texture;
            float half = column * scale / 2f;
            float soft = 1f / SlantSin;
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float center = half + (y + 0.5f) / SlantTan;
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((half - Mathf.Abs(x + 0.5f - center)) / soft + 0.5f));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void IconHeading(Rect cell, BlockColumn column, bool sorted, float lit)
        {
            float size = Mathf.Min(IconSquare, cell.width - 4f);
            Rect square = new Rect(Mathf.Round(cell.center.x - size / 2f), 4f, size, size);
            Rounded.Fill(square, Theme.BG);
            Rounded.Border(square, Color.Lerp(sorted ? Color.white : Theme.BGLight, Color.white, 0.5f * lit));
            GUI.color = sorted ? Color.white : Color.Lerp(Theme.TextDim, Color.white, lit);
            CoreWidgets.CenteredIcon(square, column.HeadingIcon, Mathf.Round(size * 0.65f));
            GUI.color = Color.white;
        }
    }
}
