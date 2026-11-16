using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class LibraryBuildTests
{
    [Fact]
    public void Tests_run_against_the_selected_library_build()
    {
        var attribute = typeof(Evaluator).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();

#if LIBRARY_NETSTANDARD2_0
        Assert.Equal(".NETStandard,Version=v2.0", attribute?.FrameworkName);
#else
        Assert.Equal(".NETCoreApp,Version=v10.0", attribute?.FrameworkName);
#endif
    }
}
