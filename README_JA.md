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
| `System.Threading.CancellationTokenExtensions` (`UnsafeRegister`) | `< .NET 5` |
| `System.Runtime.InteropServices.CollectionsMarshal` (`AsSpan`) | `< .NET 5` |
| `System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute` | `< .NET 7` |
| `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler` | `< .NET 6` |
| `DefaultInterpolatedStringHandlerExtensions` (`Text`, `Clear`) | `.NET 6～9` |
| `System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute` | `< .NET 6` |
| `System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute` | `< .NET 6` |
| `System.Runtime.CompilerServices.IsExternalInit` | `< .NET 5` |
| `System.ISpanFormattable` | `< .NET 6` |
| `System.GuidExtensions` (`Guid.CreateVersion7`) | `< .NET 9` |
| `System.Text.CompositeFormat` | `< .NET 8` |
| `System.SpanExtensions` (`Span<char>.TryWrite` の `CompositeFormat` オーバーロード) | `< .NET 8` |
| `System.Text.StringBuilderExtensions` (`StringBuilder.AppendFormat` の `CompositeFormat` オーバーロード) | `< .NET 8` |
| `System.RandomExtensions` (`Random.Shared`) | `< .NET 6` |
| `System.Runtime.CompilerServices.RequiredMemberAttribute` | `< .NET 7` |
| `System.Runtime.CompilerServices.SetsRequiredMembersAttribute` | `< .NET 7` |
| `System.Diagnostics.CodeAnalysis.StringSyntaxAttribute` | `< .NET 7` |

### API の補足説明

#### `CancellationToken.UnsafeRegister`
API シグネチャは .NET 6 の標準実装と同一ですが、挙動は UniTask の `CancellationToken.RegisterWithoutCaptureExecutionContext` と同等です。`ExecutionContext` をキャプチャせずにコールバックを登録するため、パフォーマンスが要求される場面での余分なアロケーションを回避できます。

#### `DefaultInterpolatedStringHandler`
標準の .NET ソースから実装を改変しています。`ISpanFormattable` を実装した型については、中間文字列アロケーションなしに内部バッファへ直接書き込む最適化が可能です。`Backport.Unity` 導入時は、`UnityCustomSpanFormatter` を通じて `UnityEngine.Vector3`・`Quaternion`・`Color` などの Unity 固有の値型にも対応します。

`DefaultInterpolatedStringHandler.Text` と `Clear()` は、.NET 5以前ではバックポート型の公開メンバー、.NET 6～9では拡張メンバー、.NET 10以降では標準の公開メンバーとして利用できます。.NET 6～9の `Text` プロパティ構文にはC# 14が必要です。拡張実装はランタイムの非公開メンバーに依存し、.NET 6・7ではキャッシュしたデリゲート、.NET 8・9では `UnsafeAccessor` を使用します。

#### `Random.Shared`（`RandomExtensions`）
`Random.Shared` は C# 14 の `extension` ブロックを使用して実装されています。そのため `Random.Shared` という構文が使えるのは C# 14 以上でコンパイルする場合のみです。それより前の C# バージョンでは、内部の静的アクセサー（`RandomExtensions.get_Shared()`）にはアクセスできますが、主にライブラリ内部での使用を想定しています。

#### `Guid.CreateVersion7` / `CompositeFormat`
`Guid.CreateVersion7()` と日時指定オーバーロードは、`Guid.NewGuid()` の乱数にUnixミリ秒・version・variantを設定します。Unix Epoch以前の日時は例外になります。C# 14では `Guid.CreateVersion7()`、それ以前のC#では `GuidExtensions.CreateVersion7()` と呼び出せます。

`CompositeFormat.Parse` で解析結果を再利用できます。

`Span<char>.TryWrite` はジェネリック引数1～3個・配列・`ReadOnlySpan<object?>` の5オーバーロードを提供します。容量不足なら `false` と `charsWritten = 0` を返します（出力先には途中まで書き込まれる場合があります）。

```csharp
var format = CompositeFormat.Parse("Value: {0:D4}");
Span<char> destination = stackalloc char[64];
if (destination.TryWrite(null, format, out var charsWritten, 42))
{
    var text = destination.Slice(0, charsWritten).ToString();
}
```

`StringBuilder.AppendFormat` も同じ5種類の引数形式に対応しています。.NET 8未満では、プロバイダーに型のない `null` を渡すと既存の文字列用オーバーロードが優先されるため、型のあるプロバイダー、または明示的な型引数を使用してください。

```csharp
builder.AppendFormat(CultureInfo.InvariantCulture, format, 42);
builder.AppendFormat<int>(null, format, 42);
```

`string.Format` の関連オーバーロードは提供していません。

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
