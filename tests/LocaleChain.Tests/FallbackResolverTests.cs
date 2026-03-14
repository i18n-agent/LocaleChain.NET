using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace I18nAgent.LocaleChain.Tests
{
    /// <summary>
    /// Tests for <see cref="FallbackResolver"/> — load order, chain lookup, deep merge, and async resolution.
    /// </summary>
    public class FallbackResolverTests
    {
        private readonly Dictionary<string, string[]> _defaults = FallbackMap.DefaultFallbacks;

        // -----------------------------------------------------------------
        // ChainFor tests
        // -----------------------------------------------------------------

        [Fact]
        public void ChainFor_ReturnsCorrectChain_ForPortuguese()
        {
            var resolver = new FallbackResolver(_defaults);

            Assert.Equal(new[] { "pt-PT", "pt" }, resolver.ChainFor("pt-BR"));
            Assert.Equal(new[] { "pt" }, resolver.ChainFor("pt-PT"));
        }

        [Fact]
        public void ChainFor_ReturnsEmptyArray_ForUnknownLocale()
        {
            var resolver = new FallbackResolver(_defaults);

            var chain = resolver.ChainFor("ja");

            Assert.Empty(chain);
        }

        [Fact]
        public void ChainFor_ThrowsOnNull()
        {
            var resolver = new FallbackResolver(_defaults);

            Assert.Throws<ArgumentNullException>(() => resolver.ChainFor(null!));
        }

        [Fact]
        public void ChainFor_IsCaseInsensitive()
        {
            var resolver = new FallbackResolver(_defaults);

            Assert.Equal(new[] { "pt-PT", "pt" }, resolver.ChainFor("pt-br"));
            Assert.Equal(new[] { "pt-PT", "pt" }, resolver.ChainFor("PT-BR"));
        }

        // -----------------------------------------------------------------
        // BuildLoadOrder tests
        // -----------------------------------------------------------------

        [Fact]
        public void BuildLoadOrder_StartsWithDefault_EndsWithRequestedLocale()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var order = resolver.BuildLoadOrder("pt-BR");

            Assert.Equal("en", order.First());
            Assert.Equal("pt-BR", order.Last());
        }

        [Fact]
        public void BuildLoadOrder_CorrectOrder_ForPortuguese()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var order = resolver.BuildLoadOrder("pt-BR");

            // Default first, then chain reversed (least specific first), then locale
            Assert.Equal(new List<string> { "en", "pt", "pt-PT", "pt-BR" }, order);
        }

        [Fact]
        public void BuildLoadOrder_CorrectOrder_ForSpanish()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var order = resolver.BuildLoadOrder("es-MX");

            Assert.Equal(new List<string> { "en", "es", "es-419", "es-MX" }, order);
        }

        [Fact]
        public void BuildLoadOrder_CorrectOrder_ForNorwegian()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var nnOrder = resolver.BuildLoadOrder("nn");
            Assert.Equal(new List<string> { "en", "no", "nb", "nn" }, nnOrder);

            var nbOrder = resolver.BuildLoadOrder("nb");
            Assert.Equal(new List<string> { "en", "no", "nb" }, nbOrder);
        }

        [Fact]
        public void BuildLoadOrder_NonChainLocale_ReturnsDefaultAndLocale()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var order = resolver.BuildLoadOrder("ja");

            Assert.Equal(new List<string> { "en", "ja" }, order);
        }

        [Fact]
        public void BuildLoadOrder_DefaultLocale_ReturnsJustDefault()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var order = resolver.BuildLoadOrder("en");

            Assert.Equal(new List<string> { "en" }, order);
        }

        [Fact]
        public void BuildLoadOrder_Deduplicates_WhenDefaultAppearsInChain()
        {
            // Create a chain where the default locale appears in the fallback chain
            var fallbacks = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "custom", new[] { "en" } }
            };
            var resolver = new FallbackResolver(fallbacks, "en");

            var order = resolver.BuildLoadOrder("custom");

            Assert.Equal(new List<string> { "en", "custom" }, order);
            // "en" should appear only once
            Assert.Equal(1, order.Count(l => l.Equals("en", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void BuildLoadOrder_CustomDefaultLocale()
        {
            var resolver = new FallbackResolver(_defaults, "de");

            var order = resolver.BuildLoadOrder("pt-BR");

            Assert.Equal("de", order.First());
            Assert.Equal("pt-BR", order.Last());
            Assert.Equal(new List<string> { "de", "pt", "pt-PT", "pt-BR" }, order);
        }

        [Fact]
        public void BuildLoadOrder_ThrowsOnNull()
        {
            var resolver = new FallbackResolver(_defaults);

            Assert.Throws<ArgumentNullException>(() => resolver.BuildLoadOrder(null!));
        }

        // -----------------------------------------------------------------
        // Resolve (sync) tests — deep merge priority
        // -----------------------------------------------------------------

        [Fact]
        public void Resolve_MergesInCorrectPriorityOrder()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var allMessages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "a", "en-a" }, { "b", "en-b" }, { "c", "en-c" } } },
                { "pt", new Dictionary<string, object> { { "a", "pt-a" }, { "b", "pt-b" } } },
                { "pt-PT", new Dictionary<string, object> { { "a", "pt-PT-a" } } },
                { "pt-BR", new Dictionary<string, object> { { "a", "pt-BR-a" } } }
            };

            var result = resolver.Resolve("pt-BR", allMessages);

            // pt-BR (most specific) wins for "a"
            Assert.Equal("pt-BR-a", result["a"]);
            // pt wins for "b" (pt-BR and pt-PT don't have it)
            Assert.Equal("pt-b", result["b"]);
            // en (default) provides "c" (no other locale has it)
            Assert.Equal("en-c", result["c"]);
        }

        [Fact]
        public void Resolve_SkipsMissingLocalesInMessages()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var allMessages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" } } },
                // pt-PT and pt are missing from messages
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var result = resolver.Resolve("pt-BR", allMessages);

            Assert.Equal("Oi", result["greeting"]);
        }

        [Fact]
        public void Resolve_WithEmptyMessages_ReturnsEmptyDictionary()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var result = resolver.Resolve("pt-BR", new Dictionary<string, Dictionary<string, object>>());

            Assert.Empty(result);
        }

        [Fact]
        public void Resolve_DeepMergesNestedDictionaries()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var allMessages = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "en", new Dictionary<string, object>
                    {
                        {
                            "common", new Dictionary<string, object>
                            {
                                { "ok", "OK" },
                                { "cancel", "Cancel" },
                                { "submit", "Submit" }
                            }
                        }
                    }
                },
                {
                    "pt", new Dictionary<string, object>
                    {
                        {
                            "common", new Dictionary<string, object>
                            {
                                { "ok", "OK" },
                                { "cancel", "Cancelar" }
                            }
                        }
                    }
                },
                {
                    "pt-BR", new Dictionary<string, object>
                    {
                        {
                            "common", new Dictionary<string, object>
                            {
                                { "cancel", "Cancelar (BR)" }
                            }
                        }
                    }
                }
            };

            var result = resolver.Resolve("pt-BR", allMessages);

            var common = Assert.IsType<Dictionary<string, object>>(result["common"]);
            Assert.Equal("OK", common["ok"]);                // pt wins over en (same value, but pt is loaded later)
            Assert.Equal("Cancelar (BR)", common["cancel"]); // pt-BR wins (most specific)
            Assert.Equal("Submit", common["submit"]);         // en provides (no other locale has it)
        }

        [Fact]
        public void Resolve_DeepMerge_DoesNotMutateSourceDictionaries()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var enMessages = new Dictionary<string, object>
            {
                {
                    "nav", new Dictionary<string, object>
                    {
                        { "home", "Home" },
                        { "about", "About" }
                    }
                }
            };
            var ptBrMessages = new Dictionary<string, object>
            {
                {
                    "nav", new Dictionary<string, object>
                    {
                        { "home", "Inicio" }
                    }
                }
            };

            var allMessages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", enMessages },
                { "pt-BR", ptBrMessages }
            };

            var result = resolver.Resolve("pt-BR", allMessages);

            // Source dictionaries should not be mutated
            var enNav = (Dictionary<string, object>)enMessages["nav"];
            Assert.Equal(2, enNav.Count);
            Assert.Equal("Home", enNav["home"]);

            var ptBrNav = (Dictionary<string, object>)ptBrMessages["nav"];
            Assert.Single(ptBrNav);
            Assert.Equal("Inicio", ptBrNav["home"]);

            // Result should have merged content
            var resultNav = (Dictionary<string, object>)result["nav"];
            Assert.Equal("Inicio", resultNav["home"]);
            Assert.Equal("About", resultNav["about"]);
        }

        [Fact]
        public void Resolve_ThrowsOnNullLocale()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            Assert.Throws<ArgumentNullException>(() =>
                resolver.Resolve(null!, new Dictionary<string, Dictionary<string, object>>()));
        }

        [Fact]
        public void Resolve_ThrowsOnNullMessages()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            Assert.Throws<ArgumentNullException>(() =>
                resolver.Resolve("en", null!));
        }

        // -----------------------------------------------------------------
        // ResolveAsync tests
        // -----------------------------------------------------------------

        [Fact]
        public async Task ResolveAsync_MergesCorrectly()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var store = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "a", "en-a" }, { "b", "en-b" } } },
                { "pt", new Dictionary<string, object> { { "a", "pt-a" } } },
                { "pt-BR", new Dictionary<string, object> { { "a", "pt-BR-a" } } }
            };

            var result = await resolver.ResolveAsync("pt-BR", locale =>
            {
                store.TryGetValue(locale, out var messages);
                return Task.FromResult(messages);
            });

            Assert.Equal("pt-BR-a", result["a"]);
            Assert.Equal("en-b", result["b"]);
        }

        [Fact]
        public async Task ResolveAsync_SkipsNullLoaderResults()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            var store = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" } } }
                // pt-PT, pt, pt-BR all return null
            };

            var result = await resolver.ResolveAsync("pt-BR", locale =>
            {
                store.TryGetValue(locale, out var messages);
                return Task.FromResult(messages);
            });

            Assert.Equal("Hello", result["greeting"]);
        }

        [Fact]
        public async Task ResolveAsync_ThrowsOnNullLocale()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                resolver.ResolveAsync(null!, _ => Task.FromResult<Dictionary<string, object>?>(null)));
        }

        [Fact]
        public async Task ResolveAsync_ThrowsOnNullLoader()
        {
            var resolver = new FallbackResolver(_defaults, "en");

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                resolver.ResolveAsync("en", null!));
        }

        [Fact]
        public async Task ResolveAsync_RecordsLoadedLocales()
        {
            var resolver = new FallbackResolver(_defaults, "en");
            var loadedLocales = new List<string>();

            var store = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "a", "en-a" } } },
                { "pt", new Dictionary<string, object> { { "a", "pt-a" } } },
                { "pt-BR", new Dictionary<string, object> { { "a", "pt-BR-a" } } }
            };

            await resolver.ResolveAsync("pt-BR", locale =>
            {
                loadedLocales.Add(locale);
                store.TryGetValue(locale, out var messages);
                return Task.FromResult(messages);
            });

            // Loader should be called for each locale in the load order
            Assert.Equal(new List<string> { "en", "pt", "pt-PT", "pt-BR" }, loadedLocales);
        }

        // -----------------------------------------------------------------
        // Constructor tests
        // -----------------------------------------------------------------

        [Fact]
        public void Constructor_ThrowsOnNullFallbacks()
        {
            Assert.Throws<ArgumentNullException>(() => new FallbackResolver(null!));
        }

        [Fact]
        public void Constructor_ThrowsOnNullDefaultLocale()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FallbackResolver(new Dictionary<string, string[]>(), null!));
        }

        [Fact]
        public void Constructor_DefaultsToEnglish()
        {
            var resolver = new FallbackResolver(new Dictionary<string, string[]>());

            var order = resolver.BuildLoadOrder("ja");

            Assert.Equal("en", order.First());
        }
    }
}
