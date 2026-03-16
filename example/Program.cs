using System;
using System.Collections.Generic;
using I18nAgent.LocaleChain;

// 1. Configure locale chains (uses built-in defaults)
LocaleChain.Configure();

// 2. Define inline message dictionaries
var messages = new Dictionary<string, Dictionary<string, object>>
{
    ["en"] = new()
    {
        ["greeting"] = "Hello",
        ["farewell"] = "Goodbye",
        ["welcome"] = "Welcome to LocaleChain"
    },
    ["pt"] = new()
    {
        ["greeting"] = "Olá",
        ["farewell"] = "Adeus"
    },
    ["pt-BR"] = new()
    {
        ["greeting"] = "Oi"
    }
};

// 3. Resolve messages for pt-BR with fallback chain
var resolved = LocaleChain.Resolve("pt-BR", messages);

// 4. Print results
Console.WriteLine($"greeting = \"{resolved["greeting"]}\"");
Console.WriteLine($"farewell = \"{resolved["farewell"]}\"");
Console.WriteLine($"welcome = \"{resolved["welcome"]}\"");
