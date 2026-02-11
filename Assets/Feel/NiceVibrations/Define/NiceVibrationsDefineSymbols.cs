using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
#if UNITY_EDITOR
    /// <summary>
    /// Automatically adds define symbols to the current build target.
    /// </summary>
    [InitializeOnLoad]
    public class NiceVibrationsDefineSymbols
    {
        public static readonly string[] Symbols =
        {
            "MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED"
        };

        static NiceVibrationsDefineSymbols()
        {
            TryAddDefineSymbols(Symbols);
        }

        private static void TryAddDefineSymbols(IEnumerable<string> symbolsToAdd)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;

            // Safety: Unknown build target group
            if (group == BuildTargetGroup.Unknown)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            // Unity 2023+ uses NamedBuildTarget API
            var nbt = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            string defines = PlayerSettings.GetScriptingDefineSymbols(nbt);
            var list = SplitDefines(defines);

            bool changed = false;
            foreach (var s in symbolsToAdd)
            {
                if (!list.Contains(s))
                {
                    list.Add(s);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(nbt, string.Join(";", list));
            }
#else
            // Unity 2021/2022 uses BuildTargetGroup API
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var list = SplitDefines(defines);

            bool changed = false;
            foreach (var s in symbolsToAdd)
            {
                if (!list.Contains(s))
                {
                    list.Add(s);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
            }
#endif
        }

        private static List<string> SplitDefines(string defines)
        {
            if (string.IsNullOrEmpty(defines))
            {
                return new List<string>();
            }

            return defines
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .ToList();
        }
    }
#endif
}
