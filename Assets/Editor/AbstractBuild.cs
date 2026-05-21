using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScriptedBuild
{

    abstract public class AbstractBuild
    {
        protected static List<string> scenes;

        protected static void SetScenes(List<string> newScenes)
        {
            scenes = newScenes;
        }

        protected static void Build(BuildTarget buildTarget, string filePath)
        {
            if (!scenes.Any())
            {
                UnityEngine.Debug.LogError("No Scenes.");
                return;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                target = buildTarget,
                locationPathName = filePath,
            };

            var buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ExitWithResult(buildSummary.result);
        }

        public static void BuildOptions()
        {
            var options = GetValidatedOptions();

            PlayerSettings.bundleVersion = options["buildVersion"];

            var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);

            Build(buildTarget, options["customBuildPath"]);
        }

        protected static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();
            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                var isFlag = args[current].StartsWith("-");
                if (!isFlag) continue;
                var flag = args[current].TrimStart('-');

                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var value = flagHasValue ? args[next].TrimStart('-') : "";

                providedArguments.Add(flag, value);
            }
        }

        protected static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out var validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
                EditorApplication.Exit(121);

            if (validatedOptions.TryGetValue("buildPath", out var buildPath))
                validatedOptions["customBuildPath"] = buildPath;

            if (validatedOptions.TryGetValue("customBuildPath", out _))
                return validatedOptions;

            Console.WriteLine("Missing argument -customBuildPath");
            EditorApplication.Exit(130);

            return validatedOptions;
        }

        protected static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }

}
