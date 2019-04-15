using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool
{
    [DebuggerDisplay("{ProjectName}, {RelativePath}, {ProjectGuid}")]
    public class SolutionProject
    {
        static readonly PropertyInfo SProjectInSolutionProjectName;
        static readonly PropertyInfo SProjectInSolutionRelativePath;
        static readonly PropertyInfo SProjectInSolutionProjectGuid;
        static readonly PropertyInfo SProjectInSolutionProjectType;

        static SolutionProject()
        {
            var sProjectInSolution = Type.GetType("Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, false);
            if (sProjectInSolution == null) return;

            SProjectInSolutionProjectName = sProjectInSolution.GetProperty("ProjectName", BindingFlags.NonPublic | BindingFlags.Instance);
            SProjectInSolutionRelativePath = sProjectInSolution.GetProperty("RelativePath", BindingFlags.NonPublic | BindingFlags.Instance);
            SProjectInSolutionProjectGuid = sProjectInSolution.GetProperty("ProjectGuid", BindingFlags.NonPublic | BindingFlags.Instance);
            SProjectInSolutionProjectType = sProjectInSolution.GetProperty("ProjectType", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public string ProjectName { get; }
        public string RelativePath { get; }
        public string ProjectGuid { get; }
        public string ProjectType { get; private set; }

        public SolutionProject(object solutionProject)
        {
            ProjectName = SProjectInSolutionProjectName.GetValue(solutionProject, null) as string;
            RelativePath = SProjectInSolutionRelativePath.GetValue(solutionProject, null) as string;
            ProjectGuid = SProjectInSolutionProjectGuid.GetValue(solutionProject, null) as string;
            ProjectType = SProjectInSolutionProjectType.GetValue(solutionProject, null).ToString();
        }
    }

    //internal class SolutionParser
    //Name: Microsoft.Build.Construction.SolutionParser
    //Assembly: Microsoft.Build, Version=4.0.0.0
    public class Solution
    {
        public static readonly Type SSolutionParser;
        public static readonly PropertyInfo SSolutionParserSolutionReader;
        public static readonly MethodInfo SSolutionParserParseSolution;
        public static readonly PropertyInfo SSolutionParserProjects;

        static Solution()
        {
            SSolutionParser = Type.GetType("Microsoft.Build.Construction.SolutionParser, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, false);
            if (SSolutionParser == null) return;
            SSolutionParserSolutionReader = SSolutionParser.GetProperty("SolutionReader", BindingFlags.NonPublic | BindingFlags.Instance);
            SSolutionParserProjects = SSolutionParser.GetProperty("Projects", BindingFlags.NonPublic | BindingFlags.Instance);
            SSolutionParserParseSolution = SSolutionParser.GetMethod("ParseSolution", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public List<SolutionProject> Projects { get; private set; }

        public Solution(string solutionFileName)
        {
            if (SSolutionParser != null)
            {
                var solutionParser = SSolutionParser.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .First().Invoke(null);
                using (var streamReader = new StreamReader(solutionFileName))
                {
                    SSolutionParserSolutionReader.SetValue(solutionParser, streamReader, null);
                    SSolutionParserParseSolution.Invoke(solutionParser, null);
                }

                var array = (Array)SSolutionParserProjects.GetValue(solutionParser, null);
                var projects = array.Cast<object>().Select((t, i) => new SolutionProject(array.GetValue(i))).ToList();
                Projects = projects;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Can not find type 'Microsoft.Build.Construction.SolutionParser' are you missing a assembly reference to 'Microsoft.Build.dll'?");
            }
        }
    }
}
