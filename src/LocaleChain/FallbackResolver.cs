using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18nAgent.LocaleChain
{
    /// <summary>
    /// Resolves locale fallback chains by building a load order from the configured fallback map,
    /// then deep-merging messages so more-specific locales override less-specific ones.
    /// </summary>
    public class FallbackResolver
    {
        private readonly Dictionary<string, string[]> _fallbacks;
        private readonly string _defaultLocale;

        /// <summary>
        /// Creates a new resolver with the given fallback map and default locale.
        /// </summary>
        /// <param name="fallbacks">The fallback chain map (locale -> chain of fallback locales).</param>
        /// <param name="defaultLocale">The ultimate fallback locale (e.g., "en"). Defaults to "en".</param>
        public FallbackResolver(Dictionary<string, string[]> fallbacks, string defaultLocale = "en")
        {
            _fallbacks = fallbacks ?? throw new ArgumentNullException(nameof(fallbacks));
            _defaultLocale = defaultLocale ?? throw new ArgumentNullException(nameof(defaultLocale));
        }

        /// <summary>
        /// Gets the ordered chain of fallback locales for the given locale.
        /// Returns only the fallback entries (not the locale itself).
        /// </summary>
        /// <param name="locale">The locale to look up.</param>
        /// <returns>The fallback chain, or an empty array if no chain is defined.</returns>
        public string[] ChainFor(string locale)
        {
            if (locale == null) throw new ArgumentNullException(nameof(locale));

            if (_fallbacks.TryGetValue(locale, out var chain))
            {
                return chain;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Builds the full load order for a locale. The order is:
        /// [defaultLocale, ...chain.reversed(), requestedLocale]
        /// with deduplication (first occurrence wins position, so later duplicates are removed).
        /// This means the default locale is loaded first and the requested locale is loaded last,
        /// ensuring more-specific messages override less-specific ones during deep merge.
        /// </summary>
        /// <param name="locale">The requested locale.</param>
        /// <returns>The ordered list of locales to load and merge.</returns>
        public List<string> BuildLoadOrder(string locale)
        {
            if (locale == null) throw new ArgumentNullException(nameof(locale));

            var chain = ChainFor(locale);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            // Start with default locale
            if (seen.Add(_defaultLocale))
            {
                order.Add(_defaultLocale);
            }

            // Then add chain entries in reverse order (least specific first)
            for (int i = chain.Length - 1; i >= 0; i--)
            {
                if (seen.Add(chain[i]))
                {
                    order.Add(chain[i]);
                }
            }

            // Finally add the requested locale itself (most specific, loaded last)
            if (seen.Add(locale))
            {
                order.Add(locale);
            }

            return order;
        }

        /// <summary>
        /// Resolves messages for the given locale by deep-merging from a pre-loaded set of
        /// message dictionaries. Messages are merged in load order so that more-specific
        /// locale values override less-specific ones.
        /// </summary>
        /// <param name="locale">The requested locale.</param>
        /// <param name="allMessages">A dictionary mapping locale codes to their message dictionaries.</param>
        /// <returns>A deep-merged dictionary of messages.</returns>
        public Dictionary<string, object> Resolve(
            string locale,
            Dictionary<string, Dictionary<string, object>> allMessages)
        {
            if (locale == null) throw new ArgumentNullException(nameof(locale));
            if (allMessages == null) throw new ArgumentNullException(nameof(allMessages));

            var loadOrder = BuildLoadOrder(locale);
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var loc in loadOrder)
            {
                if (allMessages.TryGetValue(loc, out var messages))
                {
                    DeepMerge(result, messages);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves messages for the given locale by loading each locale in the chain
        /// via the provided async loader, then deep-merging in load order.
        /// </summary>
        /// <param name="locale">The requested locale.</param>
        /// <param name="loader">An async function that loads messages for a given locale code.</param>
        /// <returns>A deep-merged dictionary of messages.</returns>
        public async Task<Dictionary<string, object>> ResolveAsync(
            string locale,
            Func<string, Task<Dictionary<string, object>?>> loader)
        {
            if (locale == null) throw new ArgumentNullException(nameof(locale));
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            var loadOrder = BuildLoadOrder(locale);
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var loc in loadOrder)
            {
                var messages = await loader(loc).ConfigureAwait(false);
                if (messages != null)
                {
                    DeepMerge(result, messages);
                }
            }

            return result;
        }

        /// <summary>
        /// Deep-merges <paramref name="source"/> into <paramref name="target"/>.
        /// For nested dictionaries, merging is recursive. For all other value types,
        /// the source value overwrites the target value.
        /// </summary>
        /// <param name="target">The target dictionary to merge into (modified in place).</param>
        /// <param name="source">The source dictionary to merge from.</param>
        internal static void DeepMerge(
            Dictionary<string, object> target,
            Dictionary<string, object> source)
        {
            foreach (var kvp in source)
            {
                if (kvp.Value is Dictionary<string, object> sourceNested)
                {
                    if (target.TryGetValue(kvp.Key, out var existingValue)
                        && existingValue is Dictionary<string, object> targetNested)
                    {
                        // Both sides are dictionaries — merge recursively
                        DeepMerge(targetNested, sourceNested);
                    }
                    else
                    {
                        // Source is a dict but target is not (or missing) — clone the source dict
                        target[kvp.Key] = CloneDictionary(sourceNested);
                    }
                }
                else
                {
                    // Leaf value — overwrite
                    target[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Creates a deep clone of a nested message dictionary so that mutations to the
        /// clone do not affect the original.
        /// </summary>
        private static Dictionary<string, object> CloneDictionary(Dictionary<string, object> source)
        {
            var clone = new Dictionary<string, object>(source.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in source)
            {
                if (kvp.Value is Dictionary<string, object> nested)
                {
                    clone[kvp.Key] = CloneDictionary(nested);
                }
                else
                {
                    clone[kvp.Key] = kvp.Value;
                }
            }

            return clone;
        }
    }
}
