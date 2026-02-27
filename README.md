# Backport
[![nuget](https://img.shields.io/nuget/v/AndanteTribe.Backport.svg)](https://www.nuget.org/packages/AndanteTribe.Backport/)
[![dotnet-test](https://github.com/AndanteTribe/Backport/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Backport/actions/workflows/dotnet-test.yml)
[![unity-test](https://github.com/AndanteTribe/Backport/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/Backport/actions/workflows/unity-test.yml)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Backport.svg)](./LICENSE)

English | [日本語](README_JA.md)

## Overview
**Backport** is a library that backports modern .NET APIs for use in legacy runtimes such as Unity (netstandard2.1).

It allows you to use newer C# language features and .NET APIs — such as interpolated string handlers, `required` members, `Random.Shared`, and more — even when targeting older runtimes.

### Provided APIs

| API | Available when |
|-----|---------------|
| `System.Runtime.CompilerServices.CallerArgumentExpressionAttribute` | `< .NET 6` |
| `System.Threading.CancellationTokenExtensions` (`UnsafeRegister`) | `< .NET 6` |
| `System.Runtime.InteropServices.CollectionsMarshal` (`AsSpan`) | `< .NET 5` |
| `System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute` | `< .NET 7` |
| `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler` | `< .NET 6` |
| `System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute` | `< .NET 6` |
| `System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute` | `< .NET 6` |
| `System.Runtime.CompilerServices.IsExternalInit` | `< .NET 5` |
| `System.ISpanFormattable` | `< .NET 6` |
| `System.RandomExtensions` (`Random.Shared`) | `< .NET 6` |
| `System.Runtime.CompilerServices.RequiredMemberAttribute` | `< .NET 7` |
| `System.Runtime.CompilerServices.SetsRequiredMembersAttribute` | `< .NET 7` |
| `System.Diagnostics.CodeAnalysis.StringSyntaxAttribute` | `< .NET 7` |

### API Notes

#### `CancellationToken.UnsafeRegister`
While the API signature matches the .NET 6 original, the behavior is equivalent to UniTask's `CancellationToken.RegisterWithoutCaptureExecutionContext` — it registers a callback without capturing the `ExecutionContext`, avoiding unnecessary allocations in high-performance scenarios.

#### `DefaultInterpolatedStringHandler`
The implementation has been modified from the standard .NET source. It supports optimization via `ISpanFormattable`, allowing types that implement `ISpanFormattable` to write directly into the internal buffer without intermediate string allocation. When using `Backport.Unity`, Unity-specific value types (e.g. `UnityEngine.Vector3`, `Quaternion`, `Color`, etc.) are also supported through `UnityCustomSpanFormatter`.

#### `Random.Shared` (`RandomExtensions`)
`Random.Shared` is implemented using a C# 14 `extension` block. This means `Random.Shared` syntax is only available when compiling with C# 14 or later. In earlier C# versions, the underlying static accessor (`RandomExtensions.get_Shared()`) is accessible but is primarily intended for internal library use.

## Requirements
- .NET Standard 2.1 or higher

## Installation
### NuGet (.NET)

#### .NET CLI
```ps1
dotnet add package AndanteTribe.Backport
```

#### Package Manager
```ps1
Install-Package AndanteTribe.Backport
```

### Unity
**Backport** is available as a Unity package.

#### Requirements
- Unity 2021.3 or later

#### Steps
1. Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URL:
    ```
    https://github.com/AndanteTribe/Backport.git?path=src/Backport.Unity/Packages/jp.andantetribe.backport
    ```

## License
This library is released under the [MIT License](./LICENSE).
