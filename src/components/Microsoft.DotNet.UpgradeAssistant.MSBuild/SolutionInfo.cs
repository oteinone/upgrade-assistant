﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class SolutionInfo : ISolutionInfo
    {
        private readonly Dictionary<string, string> _mappings;
        private readonly IStringHasher _hasher;

        public SolutionInfo(IStringHasher hasher, string slnFile)
        {
            _mappings = GetProjectMappings(slnFile);
            _hasher = hasher;

            if (TryGetSolutionId(slnFile, out var slnId))
            {
                SolutionId = slnId;
            }
            else
            {
                SolutionId = _hasher.Hash(slnFile);
            }
        }

        public string SolutionId { get; }

        public string GetProjectId(string project)
        {
            if (_mappings.TryGetValue(project, out var guid))
            {
                return guid;
            }

            var newGuid = _hasher.Hash(project);

            _mappings.Add(project, newGuid);

            return newGuid;
        }

        private static Dictionary<string, string> GetProjectMappings(string slnFile)
        {
            var sln = SolutionFile.Parse(slnFile);

            var projectMapping = new Dictionary<string, string>();

            foreach (var project in sln.ProjectsByGuid)
            {
                if (project.Value.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                {
                    projectMapping.Add(project.Value.AbsolutePath, project.Key);
                }
            }

            return projectMapping;
        }

        private bool TryGetSolutionId(string slnFile, [NotNullWhen(true)] out string? slnId)
        {
            var regex = new Regex(@"SolutionGuid = ({\S+})");
            var text = File.ReadAllText(slnFile);
            var matches = regex.Match(text);

            if (matches.Success)
            {
                slnId = matches.Groups[1].Value;
                return true;
            }

            slnId = null;
            return false;
        }
    }
}
