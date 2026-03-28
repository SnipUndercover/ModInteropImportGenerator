## 1.1.1
- Fix duplicate `EmbeddedAttribute` definitions by making ours `partial`

## 1.1.0
- Deprecated `DependencyNotPresent` in favor of the new `ImportFailed` `ImportState` enum
- Replaced the `UnreachableException` with `InvalidOperationException` if the import state is invalid
- Adjusted exception messages

## 1.0.0
Initial release
