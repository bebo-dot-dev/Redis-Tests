## Redis.Tests
.NET6 Redis integration tests with TestContainers

### Nuget packages in use
```
<PackageReference Include="FluentAssertions" Version="6.11.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.3" />
<PackageReference Include="NUnit" Version="3.13.3" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
<PackageReference Include="StackExchange.Redis" Version="2.6.122" />
<PackageReference Include="Testcontainers.Redis" Version="3.4.0" />
```

### Build requirements
* .NET6 SDK
* Optional: an IDE i.e. Visual Studio Code / Rider / Visual Studio

### Running the tests
```
$ dotnet test ./Redis.Tests/Redis.Tests.csproj
Determining projects to restore...
  All projects are up-to-date for restore.
  Redis.Tests -> /home/joe/Code/git-repos/Redis.Tests/Redis.Tests/bin/Debug/net6.0/Redis.Tests.dll
Test run for /home/joe/Code/git-repos/Redis.Tests/Redis.Tests/bin/Debug/net6.0/Redis.Tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.0.3+cc7fb0593127e24f55ce016fb3ac85b5b2857fec
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 1 s - /home/joe/Code/git-repos/Redis.Tests/Redis.Tests/bin/Debug/net6.0/Redis.Tests.dll (net6.0)
```