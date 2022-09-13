using CSharpSandbox.Common;
using CSharpSandbox.Parsing;
using CSharpSandbox.Wpf.Gestures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Logging;
using System.Linq;
using System.Windows.Input;

namespace CSharpSandbox.Tests
{
    class GestureParser : Parser<InputGestureTree.Stimulus[]>
    {
        public GestureParser(IMetaParser metaParser)
            : base(metaParser, "gesture")
        {
        }

        public override InputGestureTree.Stimulus[] Parse(ParseNode gesture)
        {
            var nodes = ((ParseNode)gesture[0]).ToList<ParseNode>();
            var stimuli = nodes.Select(pnode =>
                pnode.Expand(new[] { 0 }, (ParseNode? modifierNode, Token? keyName, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode? _, IParseNode[] _) =>
                {
                    var modifiers = new ModifierKeys[] { 0 }
                        .Concat(modifierNode!
                            .Select(GetModifierName)
                            .Select(m => m switch
                            {
                                "Ctrl" => ModifierKeys.Control,
                                "Alt" => ModifierKeys.Alt,
                                "Shift" => ModifierKeys.Shift,
                                "Windows" => ModifierKeys.Windows,
                                _ => throw new Exception(),
                            }))
                        .Aggregate((a, b) => a | b);

                    var wordKeys = new[] { Key.Space, Key.Tab, Key.Enter, Key.Pause, Key.Delete, Key.Insert, Key.PrintScreen }
                        .ToDictionary(k => k.ToString(), k => k);

                    var key = (string)keyName! switch
                    {
                        var x when wordKeys.TryGetValue(x, out Key temp) => temp,
                        var x when x.Length == 1 && 'A' <= x[0] && x[0] <= 'Z' => Enum.Parse<Key>(x),
                        var x when 1 < x.Length && x[0] == 'F' && int.TryParse(x[1..], out _) => Enum.Parse<Key>(x),
                        var x when 1 < x.Length && x[0] == '#' && int.TryParse(x[1..], out _) => Enum.Parse<Key>($"NumPad{x[1..]}"),
                        var x when int.TryParse(x, out _) => Enum.Parse<Key>($"D{x}"),
                        ";" => Key.OemSemicolon,
                        "'" => Key.OemQuotes,
                        "," => Key.OemComma,
                        "." => Key.OemPeriod,
                        "/" => throw new Exception(),
                        "`" => Key.OemTilde,
                        "-" => Key.OemMinus,
                        "=" => throw new Exception(),
                        "[" => Key.OemOpenBrackets,
                        "]" => Key.OemCloseBrackets,
                        _ => throw new Exception(),
                    };

                    return new InputGestureTree.Stimulus(modifiers, key);
                }))
                .ToArray();

            return stimuli;
        }

        public override string ToString(INamedRule rule, IParseNode node)
        {
            switch (node)
            {
                case Token tnode:
                    return tnode.ToString();
                case ParseNode pnode:
                    switch (rule.Name)
                    {
                        case "gesture":
                            return string.Join(" ", (ParseNode)pnode[0]);
                        case "chord":
                            pnode = (ParseNode)pnode[0];
                            var modifiers = ((ParseNode)pnode[0])
                                .Select(GetModifierName);

                            var key = (Token)pnode[1];

                            return $"{string.Join("+", modifiers)}+{key}";
                        default:
                            throw new Exception();
                    }
                default:
                    throw new Exception();
            }
        }

        private string GetModifierName(IParseNode node)
        {
            var tnode = node as Token;

            if (tnode == null && node is ParseNode pnode)
            {
                tnode = (Token)pnode[0];
            }

            if (tnode == null)
            {
                throw new Exception();
            }

            return tnode;
        }
    }

    [TestClass]
    public class WpfTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void ParseInputGestures()
        {
            using var host = GetHost();

            var grammar = @"
S = /\s+/;
modifier = /Ctrl|Alt|Shift|Windows/;
plus = ""+"";
key = /(?:F[1-9][0-9]?|Space|Tab|Enter|[][A-Z0-9!@#$%^&.\\`""'~_(){}?=+\/*-])/;

chord = (modifier plus)* key;
gesture = chord (S chord)*;
";

            var metaParserFactory = host.Services.GetRequiredService<IMetaParserFactory>();

            var metaParser = metaParserFactory.Create<GestureParser, InputGestureTree.Stimulus[]>(mp => new GestureParser(mp));

            Assert.IsTrue(metaParser.TryParse(grammar, out var parser));
            Assert.IsTrue(parser.TryParse("Ctrl+A Alt+B C", out var stimuli));
        }

        private IHost GetHost()
        {
            var host = CreateHostBuilder().Build();

            host.Start();

            host.Services.GetRequiredService<Toolbox>();

            return host;
        }

        private IHostBuilder CreateHostBuilder()
        {
            /*
            var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();
            */

            return Host.CreateDefaultBuilder()
                .ConfigureServices((builderContext, services) =>
                {
                    var env = builderContext.HostingEnvironment;

                    var config = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true)
                        .AddEnvironmentVariables()
                        .Build();

                    LogManager
                        .Setup()
                        .LoadConfigurationFromSection(config.GetSection("NLog"));

                    /*
                    var config = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .Build();
                    //*/
                    //LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));

                    /*
                    var loggerOptions = new NLogProviderOptions()
                    {
                        IgnoreEmptyEventId = true,
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true,
                        ParseMessageTemplates = true,
                        IncludeScopes = true,
                        ShutdownOnDispose = true,
                    };

                    services.AddSingleton<ILoggerProvider>(new NLogLoggerProvider(loggerOptions));
                    */

                    // https://github.com/NLog/NLog/wiki/Filtering-log-messages#semi-dynamic-routing-rules
                    // https://stackoverflow.com/questions/69120464/how-do-i-correctly-add-nlog-to-a-net-5-console-app/69123564#69123564
                    // https://stackoverflow.com/questions/47058036/nlog-not-logging-on-all-levels/47074246#47074246
                    // https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json
                    //LogManager.Configuration.Variables["consoleLevel"] = nameof(NLog.LogLevel.Trace);
                    //LogManager.ReconfigExistingLoggers();

                    services.AddSingleton<Toolbox>();

                    services.AddSingleton<IMetaParserFactory, MetaParserFactory>();
                });
        }
    }
}
