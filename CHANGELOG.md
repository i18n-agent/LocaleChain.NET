## 1.0.0

- Initial release
- Default fallback chains for Chinese, Portuguese, Spanish, French, German, Italian, Dutch, English, Arabic, Norwegian, Malay
- Three configuration modes: default, override-merge, full-custom
- Synchronous `Resolve()` with pre-loaded message dictionaries
- Asynchronous `ResolveAsync()` with lazy-loading via callback
- Deep-merge resolution -- more-specific locale values override less-specific ones
- `LocaleChainStringLocalizer` for ASP.NET Core `IStringLocalizer` integration
- `ChainFor()` and `BuildLoadOrder()` for introspection and debugging
- Thread-safe static API with double-checked locking
- Multi-target: .NET 6.0 and .NET Standard 2.0
