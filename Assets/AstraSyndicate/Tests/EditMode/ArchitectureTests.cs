using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// Enforces the module boundaries described in ARCHITECTURE.md. A failure here means a dependency was added
    /// that the architecture does not allow; fix the code (usually by publishing an event or adding an interface)
    /// rather than the table, unless the team agrees the rule should change.
    /// </summary>
    public class ArchitectureTests
    {
        private const string Core = "AstraSyndicate.Core";
        private const string Save = "AstraSyndicate.Save";
        private const string Security = "AstraSyndicate.Security";
        private const string SceneFlow = "AstraSyndicate.SceneFlow";
        private const string Campaign = "AstraSyndicate.Campaign";
        private const string Combat = "AstraSyndicate.Combat";
        private const string AI = "AstraSyndicate.AI";
        private const string PlayerControl = "AstraSyndicate.PlayerControl";
        private const string App = "AstraSyndicate.App";

        // Everything each runtime module may reference. Campaign and the combat modules never see each other: they
        // meet through Core events. App is the composition root and the only module that sees all gameplay
        // modules, but not Security: save protectors are plugged in as assets through the Save hook.
        private static readonly Dictionary<string, string[]> AllowedReferences = new Dictionary<string, string[]>
        {
            { Core, Array.Empty<string>() },
            { Save, new[] { Core } },
            { Security, new[] { Core, Save } },
            { SceneFlow, new[] { Core, "UnityEngine.UI" } },
            { Campaign, new[] { Core, Save, SceneFlow } },
            { Combat, new[] { Core } },
            { AI, new[] { Core, Combat } },
            { PlayerControl, new[] { Core, Combat, "Unity.InputSystem" } },
            { App, new[] { Core, Save, SceneFlow, Campaign, Combat, AI, PlayerControl } },
        };

        private static string ScriptsFolder => Path.Combine(Application.dataPath, "AstraSyndicate", "Scripts");

        [Test]
        public void EveryRuntimeModule_HasDeclaredDependencyRules()
        {
            foreach (AssemblyDefinition module in RuntimeModules())
            {
                Assert.IsTrue(AllowedReferences.ContainsKey(module.name), $"{module.name} is new: add it to ArchitectureTests and ARCHITECTURE.md with its allowed dependencies.");
            }
        }

        [Test]
        public void RuntimeModules_ReferenceOnlyAllowedModules()
        {
            var violations = new List<string>();
            foreach (AssemblyDefinition module in RuntimeModules())
            {
                if (!AllowedReferences.TryGetValue(module.name, out string[] allowed)) continue;
                violations.AddRange(module.references.Where(reference => !allowed.Contains(reference)).Select(reference => $"{module.name} -> {reference}"));
            }

            CollectionAssert.IsEmpty(violations, "Module boundary violations:\n" + string.Join("\n", violations));
        }

        [Test]
        public void RuntimeModules_BuildForEveryPlatform()
        {
            string[] editorOnly = RuntimeModules().Where(module => module.includePlatforms.Length > 0).Select(module => module.name).ToArray();

            CollectionAssert.IsEmpty(editorOnly, "Runtime modules must not be platform-restricted; editor code belongs in Assets/AstraSyndicate/Editor.");
        }

        [Test]
        public void RuntimeScripts_DoNotUseTheEditorApi()
        {
            string[] offenders = Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("using UnityEditor"))
                .Select(Path.GetFileName)
                .ToArray();

            CollectionAssert.IsEmpty(offenders, "Runtime scripts must not use UnityEditor; player builds would fail.");
        }

        private static IEnumerable<AssemblyDefinition> RuntimeModules()
        {
            return Directory.GetFiles(ScriptsFolder, "*.asmdef", SearchOption.AllDirectories)
                .Select(path => JsonUtility.FromJson<AssemblyDefinition>(File.ReadAllText(path)));
        }

        [Serializable]
        private class AssemblyDefinition
        {
            public string name = string.Empty;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
        }
    }
}
