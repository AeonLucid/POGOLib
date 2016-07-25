using CommandLine;
using CommandLine.Text;

namespace Demo
{
    internal class Arguments
    {
        [Option('u', "username", Required = true, HelpText = "The username of your PTC / Google account.")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "The password of your PTC / Google account.")]
        public string Password { get; set; }

        [Option('l', "login", DefaultValue = "PTC", HelpText = "Must be 'PTC' or 'Google'.")]
        public string LoginProvider { get; set; }

        [Option('d', "debug", DefaultValue = false, HelpText = "For debugging.")]
        public bool Debug { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}