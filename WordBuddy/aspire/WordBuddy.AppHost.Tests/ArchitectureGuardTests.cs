using System.Xml.Linq;
using FluentAssertions;

namespace WordBuddy.AppHost.Tests;

/// <summary>Guards ADR 0001 / 0007: the AppHost and the services stay independent of each other.</summary>
public class ArchitectureGuardTests
{
    [Fact]
    public void AppHostProject_References_HasNoProjectReferenceToServices()
    {
        string csproj = Path.Combine(AppHostFixture.FindBackendRoot(), "aspire", "WordBuddy.AppHost", "WordBuddy.AppHost.csproj");

        XDocument doc = XDocument.Load(csproj);

        doc.Descendants("ProjectReference").Should().BeEmpty();
    }

    [Fact]
    public void ServiceProjects_References_DoNotReferenceAspire()
    {
        string services = Path.Combine(AppHostFixture.FindBackendRoot(), "src", "Services");
        string obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        string bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        string[] offenders =
        [
            .. Directory.EnumerateFiles(services, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !p.Contains(obj) && !p.Contains(bin))
                .Where(p =>
                {
                    // Only ProjectReference paths count; Aspire client packages are allowed by ADR 0007.
                    return XDocument.Load(p).Descendants("ProjectReference")
                        .Select(r => (string?)r.Attribute("Include") ?? string.Empty)
                        .Any(path => path.Contains("aspire", StringComparison.OrdinalIgnoreCase)
                            || path.Contains("ServiceDefaults", StringComparison.OrdinalIgnoreCase));
                }),
        ];

        offenders.Should().BeEmpty();
    }
}
