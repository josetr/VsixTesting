# VsixTesting
[![Build status](https://ci.appveyor.com/api/projects/status/4y4ihbei7qeif8a5/branch/master?svg=true)](https://ci.appveyor.com/project/josetr/vsixtesting/branch/master)
[![codecov](https://codecov.io/gh/josetr/VsixTesting/branch/master/graph/badge.svg)](https://codecov.io/gh/josetr/VsixTesting)

VsixTesting allows you to easily test your Visual Studio Extensions.

![Image](VsixTesting.png)

## Xunit

[![#](https://img.shields.io/nuget/v/VsixTesting.Xunit.svg?style=flat)](http://www.nuget.org/packages/VsixTesting.Xunit/) [![Join the chat at https://gitter.im/VsixTesting/Lobby](https://badges.gitter.im/VsixTesting/Lobby.svg)](https://gitter.im/VsixTesting/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

.csproj
```xml
<ItemGroup>
    <PackageReference Include="xunit" Version="2.3.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.1" />
    <PackageReference Include="VsixTesting.Xunit" Version="0.1.1-beta" />
    <PackageReference Include="VSSDK.Shell.11" Version="11.0.4" />
</ItemGroup>
```

.cs
```csharp
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Tests
{  
    public class TestClass
    {
        [VsFact]
        void FactTest()
            =>  Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));

        [VsTheory]
        [InlineData(123)]
        void TheoryTest(int n)
        {
            Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));
            Assert.Equal(123, n);
        }
    }
}

```

### Test Settings 

[ITestSettings.cs](src/VsixTesting/ITestSettings.cs) implemented by
 * [VsTestSettingsAttribute.cs](src/VsixTesting.Xunit/VsTestSettingsAttribute.cs) (for assemblies/collections/classes)
 * [VsFactAttribute.cs](src/VsixTesting.Xunit/VsFactAttribute.cs) (for methods)

### Instance Settings 

[IInstanceSettings.cs](src/VsixTesting/IInstanceSettings.cs) implemented by
 * [VsInstanceAttribute.cs](src/VsixTesting.Xunit/VsInstanceAttribute.cs)

## License

This repository is licensed with the Apache, Version 2.0 license.
