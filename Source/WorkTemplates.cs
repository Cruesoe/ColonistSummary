using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Reads the work templates UI Rethemed - Work has assigned to a pawn; inert without that mod. Resolved once per session.
    [StaticConstructorOnStartup]
    public static class WorkTemplates
    {
        public const string PackageId = "LazyFridayStudio.UIRethemed.Work";
        private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static readonly bool Active = ModsConfig.IsActive(PackageId);

        // The pawn's template list, its tag ("Cook" or "Cook +1"), every name, and the first template's color
        private static Func<Pawn, object?>? all;
        private static Func<object, string?>? tagOf;
        private static Func<object, string?>? namesOf;
        private static Func<object, Color>? accentOf;

        static WorkTemplates()
        {
            if (!Active) return;
            try
            {
                Type? roles = GenTypes.GetTypeInAnyAssembly("UIRethemed.Work.Roles");
                MethodInfo? allMethod = roles?.GetMethod("All", Static, null, new[] { typeof(Pawn) }, null);
                MethodInfo? tagMethod = roles?.GetMethod("TagLabel", Static);
                MethodInfo? namesMethod = roles?.GetMethod("Names", Static);
                Type? listType = allMethod?.ReturnType;
                Type? template = listType != null && listType.IsGenericType ? listType.GetGenericArguments()[0] : null;
                PropertyInfo? accent = template?.GetProperty("Accent", typeof(Color));
                PropertyInfo? item = listType?.GetProperty("Item");
                if (allMethod == null || tagMethod == null || namesMethod == null || listType == null || accent == null || item == null)
                {
                    Log.Warning("[Colonist Summary] UI Rethemed - Work found, but its templates have changed. Work templates won't show.");
                    return;
                }

                ParameterExpression pawn = Expression.Parameter(typeof(Pawn), "pawn");
                all = Expression.Lambda<Func<Pawn, object?>>(Expression.Convert(Expression.Call(allMethod, pawn), typeof(object)), pawn).Compile();

                ParameterExpression boxed = Expression.Parameter(typeof(object), "list");
                Expression list = Expression.Convert(boxed, listType);
                tagOf = Expression.Lambda<Func<object, string?>>(Expression.Call(tagMethod, list), boxed).Compile();
                namesOf = Expression.Lambda<Func<object, string?>>(Expression.Call(namesMethod, list), boxed).Compile();
                // Only read when the tag is set, so the list has a first template
                accentOf = Expression.Lambda<Func<object, Color>>(Expression.Property(Expression.Property(list, item, Expression.Constant(0)), accent), boxed).Compile();
            }
            catch (Exception e)
            {
                all = null;
                Log.Warning("[Colonist Summary] UI Rethemed - Work found, but its templates have changed. Work templates won't show. " + e.Message);
            }
        }

        // The tag, every template's name and the tag's color; false when the pawn has none or the mod is absent
        public static bool TryGet(Pawn pawn, out string tag, out string names, out Color color)
        {
            tag = "";
            names = "";
            color = Color.white;
            if (all == null) return false;
            try
            {
                object? list = all(pawn);
                string? label = list == null ? null : tagOf!(list);
                if (label.NullOrEmpty()) return false;
                tag = label!;
                names = namesOf!(list!) ?? "";
                color = accentOf!(list!);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[Colonist Summary] Reading UI Rethemed - Work's templates failed; work templates won't show. " + e.Message);
                all = null;
                return false;
            }
        }
    }
}
