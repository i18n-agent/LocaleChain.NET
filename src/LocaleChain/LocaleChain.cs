using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18nAgent.LocaleChain
{
    /// <summary>
    /// Public API for locale fallback chain resolution.
    /// Thread-safe static class that provides zero-config defaults, custom overrides,
    /// and both synchronous and asynchronous message resolution with deep merge.
    /// </summary>
    public static class LocaleChain
    {
        private static volatile FallbackResolver? _resolver;
        private static readonly object _lock = new object();

        /// <summary>
        /// The default locale used as the ultimate fallback in the chain.
        /// </summary>
        public const string DefaultLocale = "en";

        /// <summary>
        /// Gets the current resolver instance, or null if not yet configured.
        /// </summary>
        internal static FallbackResolver? CurrentResolver => _resolver;

        /// <summary>
        /// Configures the locale chain with default fallback chains (zero-config).
        /// </summary>
        public static void Configure()
        {
            lock (_lock)
            {
                _resolver = new FallbackResolver(FallbackMap.DefaultFallbacks, DefaultLocale);
            }
        }

        /// <summary>
        /// Configures the locale chain with custom overrides merged on top of the defaults.
        /// Overrides replace default entries with the same locale key.
        /// </summary>
        /// <param name="overrides">Custom fallback overrides to merge with defaults.</param>
        public static void Configure(Dictionary<string, string[]> overrides)
        {
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));

            lock (_lock)
            {
                var merged = FallbackMap.Merge(FallbackMap.DefaultFallbacks, overrides);
                _resolver = new FallbackResolver(merged, DefaultLocale);
            }
        }

        /// <summary>
        /// Configures the locale chain with a full custom fallback map.
        /// When <paramref name="mergeDefaults"/> is true, the provided fallbacks are merged on
        /// top of the defaults. When false, only the provided fallbacks are used.
        /// </summary>
        /// <param name="fallbacks">The custom fallback map.</param>
        /// <param name="mergeDefaults">
        /// If true, merges <paramref name="fallbacks"/> on top of <see cref="FallbackMap.DefaultFallbacks"/>.
        /// If false, uses <paramref name="fallbacks"/> exclusively.
        /// </param>
        public static void Configure(Dictionary<string, string[]> fallbacks, bool mergeDefaults)
        {
            if (fallbacks == null) throw new ArgumentNullException(nameof(fallbacks));

            lock (_lock)
            {
                var map = mergeDefaults
                    ? FallbackMap.Merge(FallbackMap.DefaultFallbacks, fallbacks)
                    : new Dictionary<string, string[]>(fallbacks, StringComparer.OrdinalIgnoreCase);

                _resolver = new FallbackResolver(map, DefaultLocale);
            }
        }

        /// <summary>
        /// Resets the locale chain configuration, removing the current resolver.
        /// Primarily useful for testing.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _resolver = null;
            }
        }

        /// <summary>
        /// Resolves messages for the given locale by deep-merging from a pre-loaded set
        /// of message dictionaries, following the configured fallback chain.
        /// Auto-configures with defaults if not yet configured.
        /// </summary>
        /// <param name="locale">The requested locale (e.g., "pt-BR").</param>
        /// <param name="allMessages">A dictionary mapping locale codes to their message dictionaries.</param>
        /// <returns>A deep-merged dictionary of messages for the requested locale.</returns>
        public static Dictionary<string, object> Resolve(
            string locale,
            Dictionary<string, Dictionary<string, object>> allMessages)
        {
            return GetResolver().Resolve(locale, allMessages);
        }

        /// <summary>
        /// Resolves messages for the given locale by loading each locale in the fallback
        /// chain via the provided async loader, then deep-merging in order.
        /// Auto-configures with defaults if not yet configured.
        /// </summary>
        /// <param name="locale">The requested locale (e.g., "pt-BR").</param>
        /// <param name="loader">An async function that loads messages for a given locale code.
        /// May return null if no messages are available for that locale.</param>
        /// <returns>A deep-merged dictionary of messages for the requested locale.</returns>
        public static Task<Dictionary<string, object>> ResolveAsync(
            string locale,
            Func<string, Task<Dictionary<string, object>?>> loader)
        {
            return GetResolver().ResolveAsync(locale, loader);
        }

        /// <summary>
        /// Returns the fallback chain for a given locale (not including the locale itself).
        /// Useful for introspection and debugging.
        /// Auto-configures with defaults if not yet configured.
        /// </summary>
        /// <param name="locale">The locale to look up.</param>
        /// <returns>The ordered fallback chain for the locale, or an empty array if none is defined.</returns>
        public static string[] ChainFor(string locale)
        {
            return GetResolver().ChainFor(locale);
        }

        /// <summary>
        /// Returns the full load order for a given locale, including the default locale,
        /// fallback chain, and the locale itself.
        /// Auto-configures with defaults if not yet configured.
        /// </summary>
        /// <param name="locale">The locale to look up.</param>
        /// <returns>The ordered list of locales that would be loaded and merged.</returns>
        public static List<string> BuildLoadOrder(string locale)
        {
            return GetResolver().BuildLoadOrder(locale);
        }

        /// <summary>
        /// Gets the current resolver, auto-configuring with defaults if needed.
        /// Uses double-checked locking for thread safety.
        /// </summary>
        private static FallbackResolver GetResolver()
        {
            var resolver = _resolver;
            if (resolver != null)
            {
                return resolver;
            }

            lock (_lock)
            {
                if (_resolver == null)
                {
                    _resolver = new FallbackResolver(FallbackMap.DefaultFallbacks, DefaultLocale);
                }

                return _resolver;
            }
        }
    }
}
