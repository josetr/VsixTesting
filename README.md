# VsixTesting
[![#](https://img.shields.io/nuget/v/VsixTesting.Xunit.svg?style=flat)](http://www.nuget.org/packages/VsixTesting.Xunit/)
[![Join the chat at https://gitter.im/josetr/VsixTesting](https://badges.gitter.im/josetr/VsixTesting.svg)](https://gitter.im/josetr/VsixTesting)
[![Build status](https://ci.appveyor.com/api/projects/status/github/josetr/VsixTesting?branch=master&svg=true)](https://ci.appveyor.com/project/josetr/vsixtesting/branch/master)
[![codecov](https://codecov.io/gh/josetr/VsixTesting/branch/master/graph/badge.svg)](https://codecov.io/gh/josetr/VsixTesting)
[![MyGet](https://img.shields.io/myget/vsixtesting/v/VsixTesting.Xunit.svg)](https://www.myget.org/feed/vsixtesting/package/nuget/VsixTesting.Xunit)

VsixTesting allows you to easily test your Visual Studio Extensions. 

![Image](https://raw.githubusercontent.com/josetr/VsixTesting/master/VsixTesting.png)

## Getting Started

The fastest way to get started is cloning the [VsixTestingSamples](https://github.com/josetr/VsixTestingSamples) repo and playing with it. 

`git clone https://github.com/josetr/VsixTestingSamples`

## Create the test project

TestProject.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.3.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.1" />
    <PackageReference Include="VsixTesting.Xunit" Version="0.1.36-beta" /> 
    <!-- Optional package containing types used in this sample. -->    
    <PackageReference Include="VSSDK.Shell.11" Version="11.0.4" /> 
    <!-- 
      <ProjectReference Include="..\MyVsixProject\MyVsixProject.csproj" />      
      * VsixTesting contains an MSBuild target that scans all project references
         and if they generate a .vsix package, it will copy them 
         to the output folder where the test assembly is located.
      * VsixTesting will install all .vsix packages located next to the test assembly.
    -->        
  </ItemGroup>
</Project>
```

TestClass.cs
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

### Configuring how tests run

All the settings are located in [ITestSettings](src/VsixTesting/ITestSettings.cs) and are implemented by 3 attributes:

* `[VsTestSettings]` for classes/collections/assemblies
* `[VsFact]` and `[VsTheory]` for methods

Intellisense should be used to read the documentation for each property that can be set.

## License

This repository is licensed with the Apache, Version 2.0 license.
