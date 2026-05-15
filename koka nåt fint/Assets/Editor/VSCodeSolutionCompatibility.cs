using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public sealed class VSCodeSolutionCompatibility : AssetPostprocessor
{
    private const string CSharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

    private static void OnGeneratedCSProjectFiles()
    {
        var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectDirectory))
            return;

        var slnxPath = Directory
            .GetFiles(projectDirectory, "*.slnx", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(slnxPath) || !File.Exists(slnxPath))
            return;

        try
        {
            var slnPath = Path.ChangeExtension(slnxPath, ".sln");
            var projects = XDocument
                .Load(slnxPath)
                .Descendants()
                .Where(element => element.Name.LocalName == "Project")
                .Select(element => element.Attribute("Path")?.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('/', '\\'))
                .ToArray();

            if (projects.Length == 0)
                return;

            File.WriteAllText(slnPath, BuildSolutionText(projects), Encoding.UTF8);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to create VS Code compatible .sln file: {exception.Message}");
        }
    }

    private static string BuildSolutionText(string[] projects)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        builder.AppendLine("# Visual Studio Version 17");
        builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
        builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        foreach (var project in projects)
        {
            builder
                .Append("Project(\"")
                .Append(CSharpProjectTypeGuid)
                .Append("\") = \"")
                .Append(Path.GetFileNameWithoutExtension(project))
                .Append("\", \"")
                .Append(project)
                .Append("\", \"")
                .Append(ProjectGuid(project))
                .AppendLine("\"");
            builder.AppendLine("EndProject");
        }

        builder.AppendLine("Global");
        builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        builder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        builder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

        foreach (var project in projects)
        {
            var guid = ProjectGuid(project);
            builder.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.Build.0 = Debug|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.ActiveCfg = Release|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.Build.0 = Release|Any CPU");
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        builder.AppendLine("\t\tHideSolutionNode = FALSE");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("EndGlobal");

        return builder.ToString();
    }

    private static string ProjectGuid(string projectPath)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(projectPath.ToUpperInvariant()));

        return "{" + new Guid(hash).ToString().ToUpperInvariant() + "}";
    }
}
