using System;
using System.Collections.Generic;

namespace I18nAgent.LocaleChain
{
    /// <summary>
    /// Provides default locale fallback chains and utilities for merging custom overrides.
    /// </summary>
    public static class FallbackMap
    {
        /// <summary>
        /// The canonical set of 75 default locale fallback chains covering Chinese, Portuguese,
        /// Spanish, French, German, Italian, Dutch, English, Arabic, Norwegian, and Malay variants.
        /// </summary>
        public static Dictionary<string, string[]> DefaultFallbacks => new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Chinese Traditional
            { "zh-Hant-HK", new[] { "zh-Hant-TW", "zh-Hant" } },
            { "zh-Hant-MO", new[] { "zh-Hant-HK", "zh-Hant-TW", "zh-Hant" } },
            { "zh-Hant-TW", new[] { "zh-Hant" } },

            // Chinese Simplified
            { "zh-Hans-SG", new[] { "zh-Hans" } },
            { "zh-Hans-MY", new[] { "zh-Hans" } },

            // Portuguese
            { "pt-BR", new[] { "pt-PT", "pt" } },
            { "pt-PT", new[] { "pt" } },
            { "pt-AO", new[] { "pt-PT", "pt" } },
            { "pt-MZ", new[] { "pt-PT", "pt" } },

            // Spanish
            { "es-419", new[] { "es" } },
            { "es-MX", new[] { "es-419", "es" } },
            { "es-AR", new[] { "es-419", "es" } },
            { "es-CO", new[] { "es-419", "es" } },
            { "es-CL", new[] { "es-419", "es" } },
            { "es-PE", new[] { "es-419", "es" } },
            { "es-VE", new[] { "es-419", "es" } },
            { "es-EC", new[] { "es-419", "es" } },
            { "es-GT", new[] { "es-419", "es" } },
            { "es-CU", new[] { "es-419", "es" } },
            { "es-BO", new[] { "es-419", "es" } },
            { "es-DO", new[] { "es-419", "es" } },
            { "es-HN", new[] { "es-419", "es" } },
            { "es-PY", new[] { "es-419", "es" } },
            { "es-SV", new[] { "es-419", "es" } },
            { "es-NI", new[] { "es-419", "es" } },
            { "es-CR", new[] { "es-419", "es" } },
            { "es-PA", new[] { "es-419", "es" } },
            { "es-UY", new[] { "es-419", "es" } },
            { "es-PR", new[] { "es-419", "es" } },

            // French
            { "fr-CA", new[] { "fr" } },
            { "fr-BE", new[] { "fr" } },
            { "fr-CH", new[] { "fr" } },
            { "fr-LU", new[] { "fr" } },
            { "fr-MC", new[] { "fr" } },
            { "fr-SN", new[] { "fr" } },
            { "fr-CI", new[] { "fr" } },
            { "fr-ML", new[] { "fr" } },
            { "fr-CM", new[] { "fr" } },
            { "fr-MG", new[] { "fr" } },
            { "fr-CD", new[] { "fr" } },

            // German
            { "de-AT", new[] { "de" } },
            { "de-CH", new[] { "de" } },
            { "de-LU", new[] { "de" } },
            { "de-LI", new[] { "de" } },

            // Italian
            { "it-CH", new[] { "it" } },

            // Dutch
            { "nl-BE", new[] { "nl" } },

            // English
            { "en-GB", new[] { "en" } },
            { "en-AU", new[] { "en-GB", "en" } },
            { "en-NZ", new[] { "en-AU", "en-GB", "en" } },
            { "en-IN", new[] { "en-GB", "en" } },
            { "en-CA", new[] { "en" } },
            { "en-ZA", new[] { "en-GB", "en" } },
            { "en-IE", new[] { "en-GB", "en" } },
            { "en-SG", new[] { "en-GB", "en" } },

            // Arabic
            { "ar-SA", new[] { "ar" } },
            { "ar-EG", new[] { "ar" } },
            { "ar-AE", new[] { "ar" } },
            { "ar-MA", new[] { "ar" } },
            { "ar-DZ", new[] { "ar" } },
            { "ar-IQ", new[] { "ar" } },
            { "ar-KW", new[] { "ar" } },
            { "ar-QA", new[] { "ar" } },
            { "ar-BH", new[] { "ar" } },
            { "ar-OM", new[] { "ar" } },
            { "ar-JO", new[] { "ar" } },
            { "ar-LB", new[] { "ar" } },
            { "ar-TN", new[] { "ar" } },
            { "ar-LY", new[] { "ar" } },
            { "ar-SD", new[] { "ar" } },
            { "ar-YE", new[] { "ar" } },

            // Norwegian
            { "nb", new[] { "no" } },
            { "nn", new[] { "nb", "no" } },

            // Malay
            { "ms-MY", new[] { "ms" } },
            { "ms-SG", new[] { "ms" } },
            { "ms-BN", new[] { "ms" } },
        };

        /// <summary>
        /// Merges custom fallback overrides into a base fallback map.
        /// Entries in <paramref name="overrides"/> replace entries with the same key in <paramref name="baseMap"/>.
        /// </summary>
        /// <param name="baseMap">The base fallback map to merge into.</param>
        /// <param name="overrides">Custom overrides to apply on top of the base map.</param>
        /// <returns>A new dictionary containing the merged result.</returns>
        public static Dictionary<string, string[]> Merge(
            Dictionary<string, string[]> baseMap,
            Dictionary<string, string[]> overrides)
        {
            if (baseMap == null) throw new ArgumentNullException(nameof(baseMap));
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));

            var result = new Dictionary<string, string[]>(baseMap, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in overrides)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }
}
