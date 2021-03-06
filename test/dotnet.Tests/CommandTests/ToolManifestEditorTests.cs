// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using LocalizableStrings = Microsoft.DotNet.ToolManifest.LocalizableStrings;
using System.Linq;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolManifestEditorTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _testDirectoryRoot;
        private const string _manifestFilename = "dotnet-tools.json";

        public ToolManifestEditorTests()
        {
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
        }

        [Fact]
        public void GivenManifestFileItCanAddEntryToIt()
        {
            string manifestFile = Path.Combine(_testDirectoryRoot, _manifestFilename);
            _fileSystem.File.WriteAllText(manifestFile, _jsonContent);

            var toolManifestFileEditor = new ToolManifestEditor(_fileSystem);

            toolManifestFileEditor.Add(new FilePath(manifestFile),
                new PackageId("new-tool"),
                NuGetVersion.Parse("3.0.0"),
                new[] { new ToolCommandName("newtool") });

            _fileSystem.File.ReadAllText(manifestFile).Should().Be(
@"{
  ""version"": 1,
  ""isRoot"": true,
  ""tools"": {
    ""t-rex"": {
      ""version"": ""1.0.53"",
      ""commands"": [
        ""t-rex""
      ]
    },
    ""dotnetsay"": {
      ""version"": ""2.1.4"",
      ""commands"": [
        ""dotnetsay""
      ]
    },
    ""new-tool"": {
      ""version"": ""3.0.0"",
      ""commands"": [
        ""newtool""
      ]
    }
  }
}");
        }

        [Fact]
        public void GivenManifestFileOnSameDirectoryWhenAddingTheSamePackageIdToolItThrows()
        {
            string manifestFile = Path.Combine(_testDirectoryRoot, _manifestFilename);
            _fileSystem.File.WriteAllText(manifestFile, _jsonContent);

            var toolManifestFileEditor = new ToolManifestEditor(_fileSystem);

            PackageId packageId = new PackageId("dotnetsay");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("3.0.0");
            Action a = () => toolManifestFileEditor.Add(new FilePath(manifestFile),
                packageId,
                nuGetVersion,
                new[] {new ToolCommandName("dotnetsay")});


            var expectedString = string.Format(
                LocalizableStrings.ManifestPackageIdCollision,
                packageId.ToString(),
                nuGetVersion.ToNormalizedString(),
                manifestFile,
                packageId.ToString(),
                "2.1.4");

            a.ShouldThrow<ToolManifestException>()
                .And.Message.Should().Contain(expectedString);

            _fileSystem.File.ReadAllText(manifestFile).Should().Be(_jsonContent);
        }

        [Fact]
        public void GivenManifestFileWhenAddingTheSamePackageIdSameVersionSameCommandsItDoesNothing()
        {
            string manifestFile = Path.Combine(_testDirectoryRoot, _manifestFilename);
            _fileSystem.File.WriteAllText(manifestFile, _jsonContent);

            var toolManifestFileEditor = new ToolManifestEditor(_fileSystem);

            PackageId packageId = new PackageId("dotnetsay");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("2.1.4");
            Action a = () => toolManifestFileEditor.Add(new FilePath(manifestFile),
                packageId,
                nuGetVersion,
                new[] { new ToolCommandName("dotnetsay") });

            a.ShouldNotThrow();

            _fileSystem.File.ReadAllText(manifestFile).Should().Be(_jsonContent);
        }

        [Fact]
        public void GivenAnInvalidManifestFileOnSameDirectoryWhenAddItThrows()
        {
            string manifestFile = Path.Combine(_testDirectoryRoot, _manifestFilename);
            _fileSystem.File.WriteAllText(manifestFile, _jsonWithInvalidField);

            var toolManifestFileEditor = new ToolManifestEditor(_fileSystem);

            PackageId packageId = new PackageId("dotnetsay");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("3.0.0");
            Action a = () => toolManifestFileEditor.Add(new FilePath(manifestFile),
                packageId,
                nuGetVersion,
                new[] {new ToolCommandName("dotnetsay")});

            a.ShouldThrow<ToolManifestException>()
                .And.Message.Should().Contain(
                    string.Format(LocalizableStrings.InvalidManifestFilePrefix,
                        manifestFile,
                        string.Empty));

            _fileSystem.File.ReadAllText(manifestFile).Should().Be(_jsonWithInvalidField);
        }

        private string _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""t-rex"":{
         ""version"":""1.0.53"",
         ""commands"":[
            ""t-rex""
         ]
      },
      ""dotnetsay"":{
         ""version"":""2.1.4"",
         ""commands"":[
            ""dotnetsay""
         ]
      }
   }
}";

        private string _jsonWithInvalidField =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""t-rex"":{
         ""version"":""1.*"",
         ""commands"":[
            ""t-rex""
         ]
      }
   }
}";
    }
}
