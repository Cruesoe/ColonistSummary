using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ColonySummary
{
    // Draws rotated skill header labels as Division of Labor draws Work tab headers; returns the label rect and its matrix
    public static class HeaderLabel
    {
        private static readonly Color LabelColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.2f);

        // GUIClip is internal to Unity's IMGUI (UnityStubs on 1.7); resolved once
        private static readonly Type? clipType = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
        private static readonly Func<Vector2, Vector2>? unclip = CreateDelegate<Func<Vector2, Vector2>>("Unclip", new[] { typeof(Vector2) });
        private static readonly Func<Rect>? getTopRect = CreateDelegate<Func<Rect>>("GetTopRect", Type.EmptyTypes);

        private static T? CreateDelegate<T>(string name, Type[] parameters) where T : Delegate
        {
            MethodInfo? method = clipType?.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);
            return method == null ? null : (T)Delegate.CreateDelegate(typeof(T), method);
        }

        private static Vector2 Unclip(Vector2 position) => unclip != null ? unclip(position) : position;

        // Rotates the label 90 degrees counter-clockwise so it reads bottom to top
        public static (Rect rect, Matrix4x4 matrix) DrawVertical(Rect rect, string label)
        {
            Matrix4x4 originalMatrix = GUI.matrix;
            Rect topRect = getTopRect != null ? getTopRect() : new Rect(0f, 0f, float.MaxValue, float.MaxValue);

            // Unclip in screen space, then rotate about the clip origin
            GUI.matrix = Matrix4x4.identity;
            Vector2 origin = Unclip(Vector2.zero);
            Matrix4x4 matrix = originalMatrix
                * Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one)
                * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -90f), Vector3.one)
                * Matrix4x4.TRS(new Vector2(-rect.yMax - origin.x, rect.xMin - origin.y), Quaternion.identity, Vector3.one);
            GUI.matrix = matrix;

            // Clip to the part of the header inside the visible table
            float leftClip = Mathf.Min(rect.xMin, 0f);
            float rightClip = Mathf.Max(rect.xMax - topRect.width, 0f);
            float topClip = Mathf.Min(rect.yMin, 0f);
            float bottomClip = Mathf.Max(rect.yMax - topRect.height, 0f);
            GUI.BeginClip(new Rect(bottomClip, -leftClip, rect.height + topClip - bottomClip, rect.width + leftClip - rightClip));

            Rect labelRect = new Rect(-bottomClip + GenUI.GapTiny, leftClip, rect.height, rect.width + GenUI.GapTiny);
            DrawLabel(labelRect, label, true);

            GUI.EndClip();
            GUI.matrix = originalMatrix;
            return (labelRect, matrix);
        }

        // Rotates the label 60 degrees so its bottom-left corner sits at the bottom right of the column
        public static (Rect rect, Matrix4x4 matrix) DrawInclined(Rect rect, string label, float labelHeight)
        {
            Rect rotatedRect = new Rect(0f, 0f, rect.height, labelHeight) { center = rect.center };

            float theta = 60f * Mathf.Deg2Rad;
            Vector2 corner = new Vector2(-rotatedRect.width / 2f, -rotatedRect.height / 2f);
            float rotatedCornerX = Mathf.Cos(theta) * corner.x - Mathf.Sin(theta) * corner.y + rotatedRect.center.x;
            rotatedRect.x += rect.xMax - rotatedCornerX;

            Matrix4x4 originalMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector2 pivot = Unclip(rotatedRect.center);
            Matrix4x4 matrix = originalMatrix
                * Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one)
                * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -60f), Vector3.one)
                * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            GUI.matrix = matrix;

            DrawLabel(rotatedRect, label, false);
            Widgets.DrawLine(new Vector2(rotatedRect.xMax, rotatedRect.yMax), new Vector2(rotatedRect.xMin, rotatedRect.yMax), LineColor, 1f);

            GUI.matrix = originalMatrix;
            return (rotatedRect, matrix);
        }

        // Stacks the label's first letters top to bottom, centered at the bottom of the column
        public static Rect DrawStacked(Rect rect, string stacked, Vector2 size)
        {
            TextAnchor anchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect labelRect = new Rect(rect.center.x - size.x / 2f, rect.yMax - size.y, size.x, size.y);
            Widgets.Label(labelRect, stacked);
            Text.Anchor = anchor;
            return labelRect;
        }

        private static void DrawLabel(Rect rect, string label, bool wordWrap)
        {
            Color color = GUI.color;
            TextAnchor anchor = Text.Anchor;
            bool wrap = Text.WordWrap;
            GUI.color = LabelColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            Text.WordWrap = wordWrap;
            Widgets.Label(rect, label);
            Text.WordWrap = wrap;
            Text.Anchor = anchor;
            GUI.color = color;
        }
    }
}
