# LocaleChain for .NET

Smart locale fallback chains for .NET -- because pt-BR users deserve pt-PT, not English.

## The Problem

.NET's built-in `CultureInfo` parent chain handles simple cases (`fr-CA` -> `fr` -> invariant), but it has no support for sibling locale fallback. When `pt-BR` translations are missing, it skips `pt-PT` entirely and shows English (or whatever your default culture is).

The same thing happens with `es-MX` -> `es-419` -> `es`, `zh-Hant-HK` -> `zh-Hant-TW`, `en-AU` -> `en-GB`, and every other regional variant that has a closer sibling than the default locale.

Your users see English when a perfectly good translation exists in a sibling locale.

## The Solution

A standalone message-merging utility with built-in ASP.NET Core integration. Configures in one line. Works with `IStringLocalizer`, resource files, JSON translations, or any message format.

```csharp
// Configure once at startup
LocaleChain.Configure();

// Resolve with pre-loaded messages
var messages = new Dictionary<string, Dictionary<string, object>>
{
    ["en"] = new() { ["greeting"] = "Hello", ["farewell"] = "Goodbye" },
    ["pt"] = new() { ["greeting"] = "Ola", ["farewell"] = "Adeus" },
    ["pt-BR"] = new() { ["greeting"] = "Oi" }
};

var resolved = LocaleChain.Resolve("pt-BR", messages);
// resolved["greeting"] => "Oi"       (from pt-BR, most specific)
// resolved["farewell"] => "Adeus"    (from pt, next in chain)
```

## Installation

**NuGet**:

```bash
dotnet add package I18nAgent.LocaleChain
```

**Package Manager Console**:

```
Install-Package I18nAgent.LocaleChain
```

Targets .NET 6.0 and .NET Standard 2.0 (compatible with .NET Framework 4.6.1+).

## Quick Start: ASP.NET Core

Register `LocaleChainStringLocalizer` in your DI container to get automatic fallback resolution with `IStringLocalizer`.

```csharp
using I18nAgent.LocaleChain;
using Microsoft.Extensions.Localization;

// In Program.cs or Startup.cs
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Configure locale chains once at startup
LocaleChain.Configure();

// Register the chain-aware localizer decorator
builder.Services.AddSingleton<IStringLocalizer>(sp =>
{
    var factory = sp.GetRequiredService<IStringLocalizerFactory>();
    var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

    var inner = factory.Create(typeof(SharedResource));

    return new LocaleChainStringLocalizer(
        inner,
        locale =>
        {
            // Create a localizer for the fallback locale
            var culture = new System.Globalization.CultureInfo(locale);
            Thread.CurrentThread.CurrentUICulture = culture;
            return factory.Create(typeof(SharedResource));
        },
        currentCulture);
});
```

Then use `IStringLocalizer` as usual -- fallback happens automatically:

```csharp
public class HomeController : Controller
{
    private readonly IStringLocalizer _localizer;

    public HomeController(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    public IActionResult Index()
    {
        // If current culture is pt-BR and key is missing,
        // automatically falls back to pt-PT -> pt -> en
        ViewData["Greeting"] = _localizer["greeting"];
        return View();
    }
}
```

## Quick Start: Standalone (.NET MAUI, WPF, Console)

For applications that manage their own translation files (JSON, YAML, etc.), use the static `LocaleChain` API directly.

### Sync resolve (pre-loaded messages)

```csharp
using I18nAgent.LocaleChain;

// 1. Configure chains once at startup
LocaleChain.Configure();

// 2. Load your messages however you like
var messages = new Dictionary<string, Dictionary<string, object>>
{
    ["en"] = new() { ["greeting"] = "Hello", ["farewell"] = "Goodbye" },
    ["pt"] = new() { ["greeting"] = "Ola", ["farewell"] = "Adeus" },
    ["pt-PT"] = new() { ["greeting"] = "Ola (PT)" },
    ["pt-BR"] = new() { ["greeting"] = "Oi" }
};

// 3. Resolve with chain priority
var resolved = LocaleChain.Resolve("pt-BR", messages);
// "greeting" => "Oi"       (from pt-BR, most specific)
// "farewell" => "Adeus"    (from pt, next in chain)
```

### Async resolve (lazy loading)

```csharp
LocaleChain.Configure();

var resolved = await LocaleChain.ResolveAsync("pt-BR", async locale =>
{
    // Load messages from network, filesystem, database, etc.
    var json = await File.ReadAllTextAsync($"Translations/{locale}.json");
    return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
});
```

### .NET MAUI example

```csharp
// In MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

    // Configure locale chains
    LocaleChain.Configure();

    return builder.Build();
}

// In a ViewModel or Page
public partial class MainPage : ContentPage
{
    private Dictionary<string, object> _translations;

    public MainPage()
    {
        InitializeComponent();

        var locale = CultureInfo.CurrentUICulture.Name; // e.g., "pt-BR"
        _translations = LocaleChain.Resolve(locale, LoadAllTranslations());

        GreetingLabel.Text = _translations["greeting"]?.ToString();
    }
}
```

### WPF example

```csharp
// In App.xaml.cs
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LocaleChain.Configure();
    }
}

// In a Window or ViewModel
var locale = CultureInfo.CurrentUICulture.Name;
var messages = LocaleChain.Resolve(locale, allTranslations);
```

### Debugging

```csharp
LocaleChain.ChainFor("pt-BR");
// Returns: ["pt-PT", "pt"]

LocaleChain.BuildLoadOrder("pt-BR");
// Returns: ["en", "pt", "pt-PT", "pt-BR"]
```

## Custom Configuration

### Default (zero config)

```csharp
LocaleChain.Configure();
```

Uses all built-in fallback chains. Covers Chinese, Portuguese, Spanish, French, German, Italian, Dutch, English, Arabic, Norwegian, and Malay regional variants.

### With overrides (merge with defaults)

```csharp
// Override specific chains while keeping all defaults
LocaleChain.Configure(new Dictionary<string, string[]>
{
    ["pt-BR"] = new[] { "pt-PT", "pt" }
});
```

Your overrides replace matching keys in the default map. All other defaults remain.

### Full custom (replace defaults)

```csharp
// Full control -- only use your chains
LocaleChain.Configure(
    new Dictionary<string, string[]>
    {
        ["pt-BR"] = new[] { "pt-PT", "pt" },
        ["es-MX"] = new[] { "es-419", "es" }
    },
    mergeDefaults: false
);
```

Only the chains you specify will be active. No defaults.

## Default Fallback Map

### Chinese Traditional

| Locale | Fallback Chain |
|--------|---------------|
| zh-Hant-HK | zh-Hant-TW -> zh-Hant -> (default) |
| zh-Hant-MO | zh-Hant-HK -> zh-Hant-TW -> zh-Hant -> (default) |
| zh-Hant-TW | zh-Hant -> (default) |

### Chinese Simplified

| Locale | Fallback Chain |
|--------|---------------|
| zh-Hans-SG | zh-Hans -> (default) |
| zh-Hans-MY | zh-Hans -> (default) |

### Portuguese

| Locale | Fallback Chain |
|--------|---------------|
| pt-BR | pt-PT -> pt -> (default) |
| pt-PT | pt -> (default) |
| pt-AO | pt-PT -> pt -> (default) |
| pt-MZ | pt-PT -> pt -> (default) |

### Spanish

| Locale | Fallback Chain |
|--------|---------------|
| es-419 | es -> (default) |
| es-MX | es-419 -> es -> (default) |
| es-AR | es-419 -> es -> (default) |
| es-CO | es-419 -> es -> (default) |
| es-CL | es-419 -> es -> (default) |
| es-PE | es-419 -> es -> (default) |
| es-VE | es-419 -> es -> (default) |
| es-EC | es-419 -> es -> (default) |
| es-GT | es-419 -> es -> (default) |
| es-CU | es-419 -> es -> (default) |
| es-BO | es-419 -> es -> (default) |
| es-DO | es-419 -> es -> (default) |
| es-HN | es-419 -> es -> (default) |
| es-PY | es-419 -> es -> (default) |
| es-SV | es-419 -> es -> (default) |
| es-NI | es-419 -> es -> (default) |
| es-CR | es-419 -> es -> (default) |
| es-PA | es-419 -> es -> (default) |
| es-UY | es-419 -> es -> (default) |
| es-PR | es-419 -> es -> (default) |

### French

| Locale | Fallback Chain |
|--------|---------------|
| fr-CA | fr -> (default) |
| fr-BE | fr -> (default) |
| fr-CH | fr -> (default) |
| fr-LU | fr -> (default) |
| fr-MC | fr -> (default) |
| fr-SN | fr -> (default) |
| fr-CI | fr -> (default) |
| fr-ML | fr -> (default) |
| fr-CM | fr -> (default) |
| fr-MG | fr -> (default) |
| fr-CD | fr -> (default) |

### German

| Locale | Fallback Chain |
|--------|---------------|
| de-AT | de -> (default) |
| de-CH | de -> (default) |
| de-LU | de -> (default) |
| de-LI | de -> (default) |

### Italian

| Locale | Fallback Chain |
|--------|---------------|
| it-CH | it -> (default) |

### Dutch

| Locale | Fallback Chain |
|--------|---------------|
| nl-BE | nl -> (default) |

### English

| Locale | Fallback Chain |
|--------|---------------|
| en-GB | en -> (default) |
| en-AU | en-GB -> en -> (default) |
| en-NZ | en-AU -> en-GB -> en -> (default) |
| en-IN | en-GB -> en -> (default) |
| en-CA | en -> (default) |
| en-ZA | en-GB -> en -> (default) |
| en-IE | en-GB -> en -> (default) |
| en-SG | en-GB -> en -> (default) |

### Arabic

| Locale | Fallback Chain |
|--------|---------------|
| ar-SA | ar -> (default) |
| ar-EG | ar -> (default) |
| ar-AE | ar -> (default) |
| ar-MA | ar -> (default) |
| ar-DZ | ar -> (default) |
| ar-IQ | ar -> (default) |
| ar-KW | ar -> (default) |
| ar-QA | ar -> (default) |
| ar-BH | ar -> (default) |
| ar-OM | ar -> (default) |
| ar-JO | ar -> (default) |
| ar-LB | ar -> (default) |
| ar-TN | ar -> (default) |
| ar-LY | ar -> (default) |
| ar-SD | ar -> (default) |
| ar-YE | ar -> (default) |

### Norwegian

| Locale | Fallback Chain |
|--------|---------------|
| nb | no -> (default) |
| nn | nb -> no -> (default) |

### Malay

| Locale | Fallback Chain |
|--------|---------------|
| ms-MY | ms -> (default) |
| ms-SG | ms -> (default) |
| ms-BN | ms -> (default) |

## How It Works

1. `Configure()` stores the fallback chain configuration in a thread-safe static resolver.
2. `Resolve()` builds the full chain for the requested locale (e.g., `pt-BR -> pt-PT -> pt -> en`).
3. The chain is walked in reverse order (default locale first, most specific last).
4. Message dictionaries are deep-merged layer by layer, so more-specific locale values override less-specific ones.
5. Locales not in the fallback map get a simple two-step chain: `[defaultLocale, locale]`.
6. `ResolveAsync()` calls the loader for each locale in the chain, skipping null results.
7. `LocaleChainStringLocalizer` wraps any `IStringLocalizer` and walks the chain on cache misses.

## Deep Merge

Unlike flat key-level merge, LocaleChain for .NET supports nested message dictionaries. Nested objects are merged recursively, so you can organize translations into namespaces:

```csharp
var messages = new Dictionary<string, Dictionary<string, object>>
{
    ["en"] = new()
    {
        ["nav"] = new Dictionary<string, object>
        {
            ["home"] = "Home",
            ["about"] = "About"
        }
    },
    ["pt-BR"] = new()
    {
        ["nav"] = new Dictionary<string, object>
        {
            ["home"] = "Inicio"
            // "about" falls back to English "About"
        }
    }
};

var resolved = LocaleChain.Resolve("pt-BR", messages);
// resolved["nav"]["home"]  => "Inicio"
// resolved["nav"]["about"] => "About"
```

## FAQ

**Is this safe for production?**
Yes. The library is pure C# with no external dependencies beyond `Microsoft.Extensions.Localization.Abstractions`. Thread safety is ensured via `volatile` fields and double-checked locking.

**Performance impact?**
Negligible. Chain resolution is a simple dictionary merge. Locales not in the fallback map get a trivial two-entry chain.

**ASP.NET Core compatibility?**
Yes. Use `LocaleChainStringLocalizer` as a decorator around any `IStringLocalizer` to get automatic fallback through the chain.

**.NET MAUI / WPF / Console compatibility?**
Yes. Use the static `LocaleChain.Resolve()` or `LocaleChain.ResolveAsync()` API directly. No framework-specific dependencies required.

**Can I deactivate it?**
Yes. Call `LocaleChain.Reset()` to clear configuration.

**What if my app's default language is not English?**
The default locale is `"en"`. To change it, create a `FallbackResolver` directly with your preferred default locale and use it via the `Resolve` method.

**What .NET versions are supported?**
The package targets .NET 6.0 and .NET Standard 2.0, which covers .NET 6+, .NET Core 2.0+, and .NET Framework 4.6.1+.

## Contributing

- Open issues for bugs or feature requests.
- PRs welcome, especially for adding new locale fallback chains.
- Run `dotnet test` before submitting.

## License

MIT License - see [LICENSE](LICENSE) file.

Built by [i18nagent.ai](https://i18nagent.ai)
