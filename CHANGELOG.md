## 1.1.3
- Fix overloaded imports not working by including `[ModImportName]` in the generated import class fields
  (issue [#3](https://github.com/SnipUndercover/ModInteropImportGenerator/issues/3))
- Fix CHANGELOG.md being included in every project using our NuGet package (oops)

## 1.1.2
- Fix incorrect `MonoModImportGenerator` namespace in the README

## 1.1.1
- Fix duplicate `EmbeddedAttribute` definitions by making ours `partial`

## 1.1.0
- Deprecated `DependencyNotPresent` in favor of the new `ImportFailed` `ImportState` enum
- Replaced the `UnreachableException` with `InvalidOperationException` if the import state is invalid
- Adjusted exception messages

## 1.0.0
Initial release
