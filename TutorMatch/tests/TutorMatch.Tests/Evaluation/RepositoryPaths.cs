namespace TutorMatch.Tests.Evaluation;

/// <summary>Locates files of the repository (frontend data, docs) relative to the test binaries.</summary>
public static class RepositoryPaths
{
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "frontend"))
                    && Directory.Exists(Path.Combine(directory.FullName, "TutorMatch")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root (folder containing frontend/ and TutorMatch/).");
        }
    }

    public static string FrontendEvaluationCases =>
        Path.Combine(Root, "frontend", "src", "app", "core", "data", "evaluation-cases.ts");

    public static string Docs => Path.Combine(Root, "docs");
}
