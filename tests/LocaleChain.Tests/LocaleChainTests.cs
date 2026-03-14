using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Xunit;

namespace I18nAgent.LocaleChain.Tests
{
    /// <summary>
    /// Tests for <see cref="LocaleChain"/> (static API) and
    /// <see cref="LocaleChainStringLocalizer"/> (IStringLocalizer integration).
    /// </summary>
    public class LocaleChainTests : IDisposable
    {
        public LocaleChainTests()
        {
            // Start each test with a clean state
            LocaleChain.Reset();
        }

        public void Dispose()
        {
            LocaleChain.Reset();
        }

        // -----------------------------------------------------------------
        // Configure / Reset API
        // -----------------------------------------------------------------

        #region Configure modes

        [Fact]
        public void Configure_ZeroConfig_UsesDefaults()
        {
            LocaleChain.Configure();

            // Should be able to resolve with default chains
            var chain = LocaleChain.ChainFor("pt-BR");
            Assert.Equal(new[] { "pt-PT", "pt" }, chain);
        }

        [Fact]
        public void Configure_WithOverrides_MergesOnTopOfDefaults()
        {
            var overrides = new Dictionary<string, string[]>
            {
                { "pt-BR", new[] { "pt" } } // Shorten pt-BR chain
            };

            LocaleChain.Configure(overrides);

            // Overridden chain
            Assert.Equal(new[] { "pt" }, LocaleChain.ChainFor("pt-BR"));
            // Default chains still present
            Assert.Equal(new[] { "es-419", "es" }, LocaleChain.ChainFor("es-MX"));
        }

        [Fact]
        public void Configure_WithCustomMap_MergeDefaultsTrue_MergesCustomOntoDefaults()
        {
            var custom = new Dictionary<string, string[]>
            {
                { "custom-locale", new[] { "en" } }
            };

            LocaleChain.Configure(custom, mergeDefaults: true);

            // Custom locale is available
            Assert.Equal(new[] { "en" }, LocaleChain.ChainFor("custom-locale"));
            // Default chains also available
            Assert.Equal(new[] { "pt-PT", "pt" }, LocaleChain.ChainFor("pt-BR"));
        }

        [Fact]
        public void Configure_WithCustomMap_MergeDefaultsFalse_UsesOnlyCustom()
        {
            var custom = new Dictionary<string, string[]>
            {
                { "custom-locale", new[] { "en" } }
            };

            LocaleChain.Configure(custom, mergeDefaults: false);

            // Custom locale is available
            Assert.Equal(new[] { "en" }, LocaleChain.ChainFor("custom-locale"));
            // Default chains are NOT available — returns empty
            Assert.Empty(LocaleChain.ChainFor("pt-BR"));
        }

        [Fact]
        public void Configure_IsIdempotent()
        {
            LocaleChain.Configure();
            var chain1 = LocaleChain.ChainFor("pt-BR");

            LocaleChain.Configure();
            var chain2 = LocaleChain.ChainFor("pt-BR");

            Assert.Equal(chain1, chain2);
        }

        #endregion

        #region Reset

        [Fact]
        public void Reset_ClearsResolver()
        {
            LocaleChain.Configure();
            Assert.NotNull(LocaleChain.CurrentResolver);

            LocaleChain.Reset();
            Assert.Null(LocaleChain.CurrentResolver);
        }

        [Fact]
        public void AfterReset_AutoConfiguresOnNextResolve()
        {
            // The .NET implementation auto-configures on demand (unlike KMP which throws)
            LocaleChain.Configure();
            LocaleChain.Reset();

            // Should auto-configure and return default chain
            var chain = LocaleChain.ChainFor("pt-BR");
            Assert.Equal(new[] { "pt-PT", "pt" }, chain);
        }

        [Fact]
        public void AfterReset_ResolveAutoConfiguresWithDefaults()
        {
            LocaleChain.Configure();
            LocaleChain.Reset();

            var messages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "hello", "Hello" } } },
                { "pt-BR", new Dictionary<string, object> { { "hello", "Oi" } } }
            };

            // Auto-configures and resolves successfully
            var result = LocaleChain.Resolve("pt-BR", messages);
            Assert.Equal("Oi", result["hello"]);
        }

        #endregion

        #region Configure argument validation

        [Fact]
        public void Configure_WithNullOverrides_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                LocaleChain.Configure((Dictionary<string, string[]>)null!));
        }

        [Fact]
        public void Configure_WithNullFallbacks_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                LocaleChain.Configure(null!, mergeDefaults: true));
        }

        #endregion

        // -----------------------------------------------------------------
        // Resolve (sync) via static API
        // -----------------------------------------------------------------

        [Fact]
        public void Resolve_MergesWithChainPriority()
        {
            LocaleChain.Configure();

            var messages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" }, { "farewell", "Goodbye" } } },
                { "pt", new Dictionary<string, object> { { "greeting", "Ola" }, { "farewell", "Adeus" } } },
                { "pt-PT", new Dictionary<string, object> { { "greeting", "Ola (PT)" } } },
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var result = LocaleChain.Resolve("pt-BR", messages);

            Assert.Equal("Oi", result["greeting"]);
            Assert.Equal("Adeus", result["farewell"]);
        }

        [Fact]
        public void Resolve_NonChainLocale_MergesWithDefault()
        {
            LocaleChain.Configure();

            var messages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" }, { "farewell", "Goodbye" } } },
                { "ja", new Dictionary<string, object> { { "greeting", "Konnichiwa" } } }
            };

            var result = LocaleChain.Resolve("ja", messages);

            Assert.Equal("Konnichiwa", result["greeting"]);
            Assert.Equal("Goodbye", result["farewell"]);
        }

        [Fact]
        public void Resolve_AutoConfigures_IfNotConfigured()
        {
            // Do NOT call Configure() — should auto-configure
            var messages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" } } },
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var result = LocaleChain.Resolve("pt-BR", messages);

            Assert.Equal("Oi", result["greeting"]);
        }

        // -----------------------------------------------------------------
        // ResolveAsync via static API
        // -----------------------------------------------------------------

        [Fact]
        public async Task ResolveAsync_WorksWithLoader()
        {
            LocaleChain.Configure();

            var store = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" }, { "farewell", "Goodbye" } } },
                { "pt", new Dictionary<string, object> { { "greeting", "Ola" }, { "farewell", "Adeus" } } },
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var result = await LocaleChain.ResolveAsync("pt-BR", locale =>
            {
                store.TryGetValue(locale, out var msgs);
                return Task.FromResult(msgs);
            });

            Assert.Equal("Oi", result["greeting"]);
            Assert.Equal("Adeus", result["farewell"]);
        }

        [Fact]
        public async Task ResolveAsync_SkipsNullLoaderResults()
        {
            LocaleChain.Configure();

            var store = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" }, { "farewell", "Goodbye" } } },
                // pt-PT and pt are not in the store (loader returns null)
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var result = await LocaleChain.ResolveAsync("pt-BR", locale =>
            {
                store.TryGetValue(locale, out var msgs);
                return Task.FromResult(msgs);
            });

            Assert.Equal("Oi", result["greeting"]);
            Assert.Equal("Goodbye", result["farewell"]);
        }

        // -----------------------------------------------------------------
        // ChainFor / BuildLoadOrder via static API
        // -----------------------------------------------------------------

        [Fact]
        public void ChainFor_ReturnsCorrectChain()
        {
            LocaleChain.Configure();

            Assert.Equal(new[] { "pt-PT", "pt" }, LocaleChain.ChainFor("pt-BR"));
        }

        [Fact]
        public void ChainFor_NonChainLocale_ReturnsEmpty()
        {
            LocaleChain.Configure();

            Assert.Empty(LocaleChain.ChainFor("ja"));
        }

        [Fact]
        public void BuildLoadOrder_ReturnsFullOrder()
        {
            LocaleChain.Configure();

            var order = LocaleChain.BuildLoadOrder("pt-BR");

            Assert.Equal(new List<string> { "en", "pt", "pt-PT", "pt-BR" }, order);
        }

        [Fact]
        public void BuildLoadOrder_NonChainLocale_ReturnsDefaultAndLocale()
        {
            LocaleChain.Configure();

            var order = LocaleChain.BuildLoadOrder("ja");

            Assert.Equal(new List<string> { "en", "ja" }, order);
        }

        // -----------------------------------------------------------------
        // Thread safety
        // -----------------------------------------------------------------

        [Fact]
        public void Configure_IsThreadSafe()
        {
            const int threadCount = 20;
            var barrier = new Barrier(threadCount);
            var exceptions = new List<Exception>();

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        if (index % 3 == 0)
                        {
                            LocaleChain.Configure();
                        }
                        else if (index % 3 == 1)
                        {
                            LocaleChain.Configure(new Dictionary<string, string[]>
                            {
                                { "test", new[] { "en" } }
                            });
                        }
                        else
                        {
                            LocaleChain.Reset();
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                });
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            Assert.Empty(exceptions);
        }

        [Fact]
        public void Resolve_IsThreadSafe()
        {
            LocaleChain.Configure();

            const int threadCount = 20;
            var barrier = new Barrier(threadCount);
            var exceptions = new List<Exception>();
            var results = new Dictionary<string, object>[threadCount];

            var messages = new Dictionary<string, Dictionary<string, object>>
            {
                { "en", new Dictionary<string, object> { { "greeting", "Hello" } } },
                { "pt-BR", new Dictionary<string, object> { { "greeting", "Oi" } } }
            };

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        results[index] = LocaleChain.Resolve("pt-BR", messages);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                });
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            Assert.Empty(exceptions);

            // All threads should get the same result
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.Equal("Oi", result["greeting"]);
            }
        }

        // -----------------------------------------------------------------
        // IStringLocalizer integration
        // -----------------------------------------------------------------

        #region LocaleChainStringLocalizer

        [Fact]
        public void StringLocalizer_ReturnsValue_WhenFoundInPrimaryLocale()
        {
            LocaleChain.Configure();

            var inner = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Oi" }
            });

            var localizer = new LocaleChainStringLocalizer(inner, _ => FakeStringLocalizer.Empty, "pt-BR");

            var result = localizer["greeting"];

            Assert.False(result.ResourceNotFound);
            Assert.Equal("Oi", result.Value);
        }

        [Fact]
        public void StringLocalizer_FallsBackThroughChain_WhenMissingInPrimary()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>());
            var ptPt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Ola (PT)" }
            });
            var pt = new FakeStringLocalizer(new Dictionary<string, string>());

            var localizer = new LocaleChainStringLocalizer(
                primary,
                locale =>
                {
                    if (locale == "pt-PT") return ptPt;
                    if (locale == "pt") return pt;
                    return FakeStringLocalizer.Empty;
                },
                "pt-BR");

            var result = localizer["greeting"];

            Assert.False(result.ResourceNotFound);
            Assert.Equal("Ola (PT)", result.Value);
        }

        [Fact]
        public void StringLocalizer_ReturnsNotFound_WhenMissingInEntireChain()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>());
            var localizer = new LocaleChainStringLocalizer(
                primary,
                _ => FakeStringLocalizer.Empty,
                "pt-BR");

            var result = localizer["missing-key"];

            Assert.True(result.ResourceNotFound);
            Assert.Equal("missing-key", result.Name);
        }

        [Fact]
        public void StringLocalizer_WithArguments_FormatsFromFallback()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>());
            var ptPt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "welcome", "Bem-vindo, {0}!" }
            });

            var localizer = new LocaleChainStringLocalizer(
                primary,
                locale =>
                {
                    if (locale == "pt-PT") return ptPt;
                    return FakeStringLocalizer.Empty;
                },
                "pt-BR");

            var result = localizer["welcome", "Maria"];

            Assert.False(result.ResourceNotFound);
            Assert.Equal("Bem-vindo, Maria!", result.Value);
        }

        [Fact]
        public void StringLocalizer_WithArguments_ReturnsNotFound_WhenMissingEverywhere()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>());
            var localizer = new LocaleChainStringLocalizer(
                primary,
                _ => FakeStringLocalizer.Empty,
                "pt-BR");

            var result = localizer["missing-key", "arg1"];

            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        public void StringLocalizer_GetAllStrings_WithoutParentCultures_ReturnsOnlyPrimary()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Oi" }
            });
            var ptPt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "farewell", "Adeus (PT)" }
            });

            var localizer = new LocaleChainStringLocalizer(
                primary,
                locale =>
                {
                    if (locale == "pt-PT") return ptPt;
                    return FakeStringLocalizer.Empty;
                },
                "pt-BR");

            var strings = localizer.GetAllStrings(includeParentCultures: false).ToList();

            Assert.Single(strings);
            Assert.Equal("greeting", strings[0].Name);
        }

        [Fact]
        public void StringLocalizer_GetAllStrings_WithParentCultures_IncludesFallbackStrings()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Oi" }
            });
            var ptPt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "farewell", "Adeus (PT)" },
                { "greeting", "Ola (PT)" } // Should be deduplicated — primary wins
            });
            var pt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "submit", "Enviar" }
            });

            var localizer = new LocaleChainStringLocalizer(
                primary,
                locale =>
                {
                    if (locale == "pt-PT") return ptPt;
                    if (locale == "pt") return pt;
                    return FakeStringLocalizer.Empty;
                },
                "pt-BR");

            var strings = localizer.GetAllStrings(includeParentCultures: true).ToList();

            // 3 unique keys: greeting (from primary), farewell (from pt-PT), submit (from pt)
            Assert.Equal(3, strings.Count);
            Assert.Contains(strings, s => s.Name == "greeting" && s.Value == "Oi");
            Assert.Contains(strings, s => s.Name == "farewell" && s.Value == "Adeus (PT)");
            Assert.Contains(strings, s => s.Name == "submit" && s.Value == "Enviar");
        }

        [Fact]
        public void StringLocalizer_GetAllStrings_Deduplicates_PrimaryWins()
        {
            LocaleChain.Configure();

            var primary = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Oi" }
            });
            var ptPt = new FakeStringLocalizer(new Dictionary<string, string>
            {
                { "greeting", "Ola (PT)" }
            });

            var localizer = new LocaleChainStringLocalizer(
                primary,
                locale =>
                {
                    if (locale == "pt-PT") return ptPt;
                    return FakeStringLocalizer.Empty;
                },
                "pt-BR");

            var strings = localizer.GetAllStrings(includeParentCultures: true).ToList();

            // Only one "greeting" entry, from primary
            Assert.Single(strings);
            Assert.Equal("Oi", strings[0].Value);
        }

        #endregion

        #region StringLocalizer constructor validation

        [Fact]
        public void StringLocalizer_ThrowsOnNullInner()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LocaleChainStringLocalizer(null!, _ => FakeStringLocalizer.Empty, "en"));
        }

        [Fact]
        public void StringLocalizer_ThrowsOnNullFactory()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LocaleChainStringLocalizer(FakeStringLocalizer.Empty, null!, "en"));
        }

        [Fact]
        public void StringLocalizer_ThrowsOnNullLocale()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LocaleChainStringLocalizer(FakeStringLocalizer.Empty, _ => FakeStringLocalizer.Empty, null!));
        }

        #endregion

        #region StringLocalizer indexer null checks

        [Fact]
        public void StringLocalizer_Indexer_ThrowsOnNullName()
        {
            LocaleChain.Configure();
            var localizer = new LocaleChainStringLocalizer(
                FakeStringLocalizer.Empty,
                _ => FakeStringLocalizer.Empty,
                "en");

            Assert.Throws<ArgumentNullException>(() => localizer[null!]);
        }

        [Fact]
        public void StringLocalizer_IndexerWithArgs_ThrowsOnNullName()
        {
            LocaleChain.Configure();
            var localizer = new LocaleChainStringLocalizer(
                FakeStringLocalizer.Empty,
                _ => FakeStringLocalizer.Empty,
                "en");

            Assert.Throws<ArgumentNullException>(() => localizer[null!, "arg"]);
        }

        #endregion

        // -----------------------------------------------------------------
        // Fake IStringLocalizer for testing
        // -----------------------------------------------------------------

        /// <summary>
        /// A minimal fake <see cref="IStringLocalizer"/> that returns translations from
        /// an in-memory dictionary. Keys not in the dictionary are reported as not found.
        /// </summary>
        private class FakeStringLocalizer : IStringLocalizer
        {
            private readonly Dictionary<string, string> _resources;

            public static readonly FakeStringLocalizer Empty =
                new FakeStringLocalizer(new Dictionary<string, string>());

            public FakeStringLocalizer(Dictionary<string, string> resources)
            {
                _resources = resources;
            }

            public LocalizedString this[string name]
            {
                get
                {
                    if (_resources.TryGetValue(name, out var value))
                    {
                        return new LocalizedString(name, value, resourceNotFound: false);
                    }
                    return new LocalizedString(name, name, resourceNotFound: true);
                }
            }

            public LocalizedString this[string name, params object[] arguments]
            {
                get
                {
                    if (_resources.TryGetValue(name, out var value))
                    {
                        var formatted = string.Format(value, arguments);
                        return new LocalizedString(name, formatted, resourceNotFound: false);
                    }
                    return new LocalizedString(name, name, resourceNotFound: true);
                }
            }

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                return _resources.Select(kvp =>
                    new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
            }
        }
    }
}
