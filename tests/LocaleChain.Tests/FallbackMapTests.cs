using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace I18nAgent.LocaleChain.Tests
{
    /// <summary>
    /// Tests for <see cref="FallbackMap"/> — chain data correctness and merge behavior.
    /// </summary>
    public class FallbackMapTests
    {
        private readonly Dictionary<string, string[]> _defaults = FallbackMap.DefaultFallbacks;

        // -----------------------------------------------------------------
        // Chain data: language group correctness
        // -----------------------------------------------------------------

        #region Chinese chains

        [Fact]
        public void ChineseTraditional_ChainsAreCorrect()
        {
            Assert.Equal(new[] { "zh-Hant-TW", "zh-Hant" }, _defaults["zh-Hant-HK"]);
            Assert.Equal(new[] { "zh-Hant-HK", "zh-Hant-TW", "zh-Hant" }, _defaults["zh-Hant-MO"]);
            Assert.Equal(new[] { "zh-Hant" }, _defaults["zh-Hant-TW"]);
        }

        [Fact]
        public void ChineseSimplified_ChainsAreCorrect()
        {
            Assert.Equal(new[] { "zh-Hans" }, _defaults["zh-Hans-SG"]);
            Assert.Equal(new[] { "zh-Hans" }, _defaults["zh-Hans-MY"]);
        }

        [Fact]
        public void Chinese_HasExpectedEntryCount()
        {
            var chineseEntries = _defaults.Keys
                .Where(k => k.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 3 traditional + 2 simplified = 5
            Assert.Equal(5, chineseEntries.Count);
        }

        #endregion

        #region Portuguese chains

        [Fact]
        public void Portuguese_ChainsAreCorrect()
        {
            Assert.Equal(new[] { "pt-PT", "pt" }, _defaults["pt-BR"]);
            Assert.Equal(new[] { "pt" }, _defaults["pt-PT"]);
            Assert.Equal(new[] { "pt-PT", "pt" }, _defaults["pt-AO"]);
            Assert.Equal(new[] { "pt-PT", "pt" }, _defaults["pt-MZ"]);
        }

        [Fact]
        public void Portuguese_HasExpectedEntryCount()
        {
            var ptEntries = _defaults.Keys
                .Where(k => k.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(4, ptEntries.Count);
        }

        #endregion

        #region Spanish chains

        [Fact]
        public void Spanish_RegionalChainIsCorrect()
        {
            Assert.Equal(new[] { "es" }, _defaults["es-419"]);
        }

        [Theory]
        [InlineData("es-MX")]
        [InlineData("es-AR")]
        [InlineData("es-CO")]
        [InlineData("es-CL")]
        [InlineData("es-PE")]
        [InlineData("es-VE")]
        [InlineData("es-EC")]
        [InlineData("es-GT")]
        [InlineData("es-CU")]
        [InlineData("es-BO")]
        [InlineData("es-DO")]
        [InlineData("es-HN")]
        [InlineData("es-PY")]
        [InlineData("es-SV")]
        [InlineData("es-NI")]
        [InlineData("es-CR")]
        [InlineData("es-PA")]
        [InlineData("es-UY")]
        [InlineData("es-PR")]
        public void Spanish_LatinAmericanLocale_FallsBackThroughEs419(string locale)
        {
            Assert.Equal(new[] { "es-419", "es" }, _defaults[locale]);
        }

        [Fact]
        public void Spanish_HasExpectedEntryCount()
        {
            // 19 Latin American locales + es-419 = 20 Spanish entries
            var spanishEntries = _defaults.Keys
                .Where(k => k.StartsWith("es", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(20, spanishEntries.Count);
        }

        #endregion

        #region French chains

        [Theory]
        [InlineData("fr-CA")]
        [InlineData("fr-BE")]
        [InlineData("fr-CH")]
        [InlineData("fr-LU")]
        [InlineData("fr-MC")]
        [InlineData("fr-SN")]
        [InlineData("fr-CI")]
        [InlineData("fr-ML")]
        [InlineData("fr-CM")]
        [InlineData("fr-MG")]
        [InlineData("fr-CD")]
        public void French_Locale_FallsBackToFr(string locale)
        {
            Assert.Equal(new[] { "fr" }, _defaults[locale]);
        }

        [Fact]
        public void French_HasExpectedEntryCount()
        {
            var frenchEntries = _defaults.Keys
                .Where(k => k.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(11, frenchEntries.Count);
        }

        #endregion

        #region German chains

        [Theory]
        [InlineData("de-AT")]
        [InlineData("de-CH")]
        [InlineData("de-LU")]
        [InlineData("de-LI")]
        public void German_Locale_FallsBackToDe(string locale)
        {
            Assert.Equal(new[] { "de" }, _defaults[locale]);
        }

        [Fact]
        public void German_HasExpectedEntryCount()
        {
            var germanEntries = _defaults.Keys
                .Where(k => k.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(4, germanEntries.Count);
        }

        #endregion

        #region Italian chains

        [Fact]
        public void Italian_ChainIsCorrect()
        {
            Assert.Equal(new[] { "it" }, _defaults["it-CH"]);
        }

        #endregion

        #region Dutch chains

        [Fact]
        public void Dutch_ChainIsCorrect()
        {
            Assert.Equal(new[] { "nl" }, _defaults["nl-BE"]);
        }

        #endregion

        #region English chains

        [Fact]
        public void English_ChainsAreCorrect()
        {
            Assert.Equal(new[] { "en" }, _defaults["en-GB"]);
            Assert.Equal(new[] { "en-GB", "en" }, _defaults["en-AU"]);
            Assert.Equal(new[] { "en-AU", "en-GB", "en" }, _defaults["en-NZ"]);
            Assert.Equal(new[] { "en-GB", "en" }, _defaults["en-IN"]);
            Assert.Equal(new[] { "en" }, _defaults["en-CA"]);
            Assert.Equal(new[] { "en-GB", "en" }, _defaults["en-ZA"]);
            Assert.Equal(new[] { "en-GB", "en" }, _defaults["en-IE"]);
            Assert.Equal(new[] { "en-GB", "en" }, _defaults["en-SG"]);
        }

        [Fact]
        public void English_HasExpectedEntryCount()
        {
            var englishEntries = _defaults.Keys
                .Where(k => k.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(8, englishEntries.Count);
        }

        #endregion

        #region Arabic chains

        [Theory]
        [InlineData("ar-SA")]
        [InlineData("ar-EG")]
        [InlineData("ar-AE")]
        [InlineData("ar-MA")]
        [InlineData("ar-DZ")]
        [InlineData("ar-IQ")]
        [InlineData("ar-KW")]
        [InlineData("ar-QA")]
        [InlineData("ar-BH")]
        [InlineData("ar-OM")]
        [InlineData("ar-JO")]
        [InlineData("ar-LB")]
        [InlineData("ar-TN")]
        [InlineData("ar-LY")]
        [InlineData("ar-SD")]
        [InlineData("ar-YE")]
        public void Arabic_Locale_FallsBackToAr(string locale)
        {
            Assert.Equal(new[] { "ar" }, _defaults[locale]);
        }

        [Fact]
        public void Arabic_HasExpectedEntryCount()
        {
            var arabicEntries = _defaults.Keys
                .Where(k => k.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(16, arabicEntries.Count);
        }

        #endregion

        #region Norwegian chains

        [Fact]
        public void Norwegian_ChainsAreCorrect()
        {
            Assert.Equal(new[] { "no" }, _defaults["nb"]);
            Assert.Equal(new[] { "nb", "no" }, _defaults["nn"]);
        }

        #endregion

        #region Malay chains

        [Theory]
        [InlineData("ms-MY")]
        [InlineData("ms-SG")]
        [InlineData("ms-BN")]
        public void Malay_Locale_FallsBackToMs(string locale)
        {
            Assert.Equal(new[] { "ms" }, _defaults[locale]);
        }

        [Fact]
        public void Malay_HasExpectedEntryCount()
        {
            var malayEntries = _defaults.Keys
                .Where(k => k.StartsWith("ms", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(3, malayEntries.Count);
        }

        #endregion

        // -----------------------------------------------------------------
        // Chain data: structural invariants
        // -----------------------------------------------------------------

        [Fact]
        public void AllLanguageGroups_ArePresentInDefaults()
        {
            var expectedPrefixes = new[] { "zh", "pt", "es", "fr", "de", "it", "nl", "en", "ar", "nb", "nn", "ms" };

            foreach (var prefix in expectedPrefixes)
            {
                Assert.True(
                    _defaults.Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        || k.Equals(prefix, StringComparison.OrdinalIgnoreCase)),
                    $"Expected language group '{prefix}' to be present in defaults");
            }
        }

        [Fact]
        public void DefaultFallbacks_HasExpectedTotalCount()
        {
            // 5 Chinese + 4 Portuguese + 20 Spanish + 11 French + 4 German
            // + 1 Italian + 1 Dutch + 8 English + 16 Arabic + 2 Norwegian + 3 Malay = 75
            Assert.Equal(75, _defaults.Count);
        }

        [Fact]
        public void NoChain_IsEmpty()
        {
            foreach (var kvp in _defaults)
            {
                Assert.True(
                    kvp.Value.Length > 0,
                    $"Chain for '{kvp.Key}' should not be empty");
            }
        }

        [Fact]
        public void NoChain_ContainsSelfReference()
        {
            foreach (var kvp in _defaults)
            {
                Assert.DoesNotContain(kvp.Key, kvp.Value);
            }
        }

        [Fact]
        public void NoChain_HasIndirectCyclicReference()
        {
            foreach (var kvp in _defaults)
            {
                foreach (var fallback in kvp.Value)
                {
                    if (_defaults.TryGetValue(fallback, out var fallbackChain))
                    {
                        Assert.False(
                            fallbackChain.Contains(kvp.Key),
                            $"Chain for '{fallback}' should not reference '{kvp.Key}' (indirect cycle)");
                    }
                }
            }
        }

        [Fact]
        public void DefaultFallbacks_IsCaseInsensitive()
        {
            // The dictionary uses OrdinalIgnoreCase comparer
            var defaults = FallbackMap.DefaultFallbacks;

            Assert.True(defaults.ContainsKey("pt-br"));
            Assert.True(defaults.ContainsKey("PT-BR"));
            Assert.True(defaults.ContainsKey("Pt-Br"));
        }

        [Fact]
        public void DefaultFallbacks_ReturnsNewInstanceEachTime()
        {
            var first = FallbackMap.DefaultFallbacks;
            var second = FallbackMap.DefaultFallbacks;

            Assert.NotSame(first, second);
        }

        // -----------------------------------------------------------------
        // Merge behavior
        // -----------------------------------------------------------------

        [Fact]
        public void Merge_OverridesReplaceMatchingKeys()
        {
            var overrides = new Dictionary<string, string[]>
            {
                { "pt-BR", new[] { "pt" } }
            };

            var merged = FallbackMap.Merge(_defaults, overrides);

            // Override replaces pt-BR chain
            Assert.Equal(new[] { "pt" }, merged["pt-BR"]);
        }

        [Fact]
        public void Merge_PreservesNonMatchingDefaults()
        {
            var overrides = new Dictionary<string, string[]>
            {
                { "pt-BR", new[] { "pt" } }
            };

            var merged = FallbackMap.Merge(_defaults, overrides);

            // Other chains remain unchanged
            Assert.Equal(new[] { "pt" }, merged["pt-PT"]);
            Assert.Equal(new[] { "es-419", "es" }, merged["es-MX"]);
        }

        [Fact]
        public void Merge_AddsNewLocales()
        {
            var overrides = new Dictionary<string, string[]>
            {
                { "custom-locale", new[] { "en-GB", "en" } }
            };

            var merged = FallbackMap.Merge(_defaults, overrides);

            Assert.Equal(new[] { "en-GB", "en" }, merged["custom-locale"]);
            // Original defaults still present
            Assert.True(merged.ContainsKey("pt-BR"));
            Assert.True(merged.ContainsKey("es-MX"));
        }

        [Fact]
        public void Merge_DoesNotMutateBaseMap()
        {
            var baseMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "pt-BR", new[] { "pt-PT", "pt" } }
            };

            var overrides = new Dictionary<string, string[]>
            {
                { "pt-BR", new[] { "pt" } },
                { "new-locale", new[] { "en" } }
            };

            var merged = FallbackMap.Merge(baseMap, overrides);

            // Base map should be unchanged
            Assert.Equal(new[] { "pt-PT", "pt" }, baseMap["pt-BR"]);
            Assert.False(baseMap.ContainsKey("new-locale"));

            // Merged map has overrides
            Assert.Equal(new[] { "pt" }, merged["pt-BR"]);
            Assert.True(merged.ContainsKey("new-locale"));
        }

        [Fact]
        public void Merge_ResultIsCaseInsensitive()
        {
            var baseMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "pt-BR", new[] { "pt" } }
            };

            var overrides = new Dictionary<string, string[]>
            {
                { "en-AU", new[] { "en" } }
            };

            var merged = FallbackMap.Merge(baseMap, overrides);

            Assert.True(merged.ContainsKey("pt-br"));
            Assert.True(merged.ContainsKey("EN-AU"));
        }

        [Fact]
        public void Merge_WithEmptyOverrides_ReturnsBaseMapCopy()
        {
            var baseMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "pt-BR", new[] { "pt-PT", "pt" } }
            };

            var merged = FallbackMap.Merge(baseMap, new Dictionary<string, string[]>());

            Assert.Equal(new[] { "pt-PT", "pt" }, merged["pt-BR"]);
            Assert.NotSame(baseMap, merged);
        }

        [Fact]
        public void Merge_ThrowsOnNullBaseMap()
        {
            Assert.Throws<ArgumentNullException>(() =>
                FallbackMap.Merge(null!, new Dictionary<string, string[]>()));
        }

        [Fact]
        public void Merge_ThrowsOnNullOverrides()
        {
            Assert.Throws<ArgumentNullException>(() =>
                FallbackMap.Merge(new Dictionary<string, string[]>(), null!));
        }
    }
}
