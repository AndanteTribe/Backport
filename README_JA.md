# Backport
[![nuget](https://img.shields.io/nuget/v/AndanteTribe.Backport.svg)](https://www.nuget.org/packages/AndanteTribe.Backport/)
[![dotnet-test](https://github.com/AndanteTribe/Backport/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Backport/actions/workflows/dotnet-test.yml)
[![unity-test](https://github.com/AndanteTribe/Backport/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/Backport/actions/workflows/unity-test.yml)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Backport.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要
**Backport** は、Unity (netstandard2.1) などのレガシーランタイム向けに、モダンな .NET API をバックポートするライブラリです。

補間文字列ハンドラー、`required` メンバー、`Random.Shared` など、新しい C# 言語機能や .NET API を古いランタイムをターゲットにしている場合でも利用できるようになります。

### 提供する API

| API | 利用可能な条件 |
|-----|--------------|
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

## 動作要件
- .NET Standard 2.1 以上

## インストール
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
**Backport** は Unity パッケージとして利用可能です。

#### 動作要件
- Unity 2021.3 以上

#### 手順
1. `Window > Package Manager` を開き、`[+] > Add package from git URL` を選択して、以下の URL を入力してください:
    ```
    https://github.com/AndanteTribe/Backport.git?path=src/Backport.Unity/Packages/jp.andantetribe.backport
    ```

## ライセンス
このライブラリは [MIT ライセンス](./LICENSE) のもとで公開されています。
