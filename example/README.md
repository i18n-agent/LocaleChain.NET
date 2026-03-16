# LocaleChain.NET Example

Minimal console app demonstrating locale fallback chain resolution.

## What it does

Resolves messages for `pt-BR` using inline dictionaries:

- `en`: greeting, farewell, welcome
- `pt`: greeting, farewell
- `pt-BR`: greeting only

The fallback chain for `pt-BR` is: `pt-BR -> pt-PT -> pt -> en`

## Run

```bash
cd example
dotnet run
```

## Expected output

```
greeting = "Oi"           # from pt-BR (most specific)
farewell = "Adeus"        # from pt (fallback)
welcome = "Welcome to LocaleChain"  # from en (ultimate fallback)
```
