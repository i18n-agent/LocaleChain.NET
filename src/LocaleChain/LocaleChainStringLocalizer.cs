using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace I18nAgent.LocaleChain
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> implementation that wraps an inner localizer and
    /// walks the locale fallback chain to find translations for missing keys.
    /// This enables automatic fallback resolution within ASP.NET Core's localization pipeline.
    /// </summary>
    public class LocaleChainStringLocalizer : IStringLocalizer
    {
        private readonly IStringLocalizer _inner;
        private readonly Func<string, IStringLocalizer> _localizerFactory;
        private readonly string _locale;

        /// <summary>
        /// Creates a new <see cref="LocaleChainStringLocalizer"/>.
        /// </summary>
        /// <param name="inner">The inner localizer for the primary locale.</param>
        /// <param name="localizerFactory">
        /// A factory function that creates an <see cref="IStringLocalizer"/> for a given locale code.
        /// Used to load localizers for fallback locales in the chain.
        /// </param>
        /// <param name="locale">The primary locale code (e.g., "en-AU").</param>
        public LocaleChainStringLocalizer(
            IStringLocalizer inner,
            Func<string, IStringLocalizer> localizerFactory,
            string locale)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _localizerFactory = localizerFactory ?? throw new ArgumentNullException(nameof(localizerFactory));
            _locale = locale ?? throw new ArgumentNullException(nameof(locale));
        }

        /// <summary>
        /// Gets the string resource with the given name. If the resource is not found in
        /// the primary locale, walks the fallback chain to find it.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <returns>The localized string, or a resource-not-found result if not found in any locale.</returns>
        public LocalizedString this[string name]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                // Try the primary locale first
                var result = _inner[name];
                if (!result.ResourceNotFound)
                {
                    return result;
                }

                // Walk the fallback chain
                return FindInChain(name);
            }
        }

        /// <summary>
        /// Gets the string resource with the given name and format arguments.
        /// If the resource is not found in the primary locale, walks the fallback chain.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="arguments">The values to format the string with.</param>
        /// <returns>The formatted localized string, or a resource-not-found result.</returns>
        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                // Try the primary locale first
                var result = _inner[name, arguments];
                if (!result.ResourceNotFound)
                {
                    return result;
                }

                // Walk the fallback chain
                return FindInChain(name, arguments);
            }
        }

        /// <summary>
        /// Gets all string resources from the primary locale.
        /// </summary>
        /// <param name="includeParentCultures">
        /// Whether to include strings from parent cultures.
        /// </param>
        /// <returns>All localized strings.</returns>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            // Collect strings from all locales in the chain (most specific first)
            if (!includeParentCultures)
            {
                return _inner.GetAllStrings(false);
            }

            var chain = LocaleChain.ChainFor(_locale);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allStrings = new List<LocalizedString>();

            // Primary locale first
            foreach (var str in _inner.GetAllStrings(false))
            {
                if (seen.Add(str.Name))
                {
                    allStrings.Add(str);
                }
            }

            // Then walk the fallback chain
            foreach (var fallbackLocale in chain)
            {
                var fallbackLocalizer = _localizerFactory(fallbackLocale);
                foreach (var str in fallbackLocalizer.GetAllStrings(false))
                {
                    if (seen.Add(str.Name))
                    {
                        allStrings.Add(str);
                    }
                }
            }

            return allStrings;
        }

        /// <summary>
        /// Walks the fallback chain to find a localized string by name.
        /// </summary>
        private LocalizedString FindInChain(string name, object[]? arguments = null)
        {
            var chain = LocaleChain.ChainFor(_locale);

            foreach (var fallbackLocale in chain)
            {
                var fallbackLocalizer = _localizerFactory(fallbackLocale);
                var result = arguments != null
                    ? fallbackLocalizer[name, arguments]
                    : fallbackLocalizer[name];

                if (!result.ResourceNotFound)
                {
                    return result;
                }
            }

            // Not found in any fallback — return a not-found result
            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }
}
