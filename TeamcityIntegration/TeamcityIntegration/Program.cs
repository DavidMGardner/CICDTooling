using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using ToolingAutomation;

namespace TeamcityIntegration
{
    [HelpOption]
    public class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "TeamCityIntegration",
                Description = "TeamCityIntegration for handling TeamCity CI/CD Automation.",
                ThrowOnUnexpectedArgument = false
            };


            CommitChanges.AddCommand(app);
          

            app.HelpOption("-? | -h | --help");

            try
            {
                // parse and call OnExecute handler specified above
                var result = app.Execute(args);
                Environment.Exit(result);
            }
            catch (CommandParsingException ex)
            {
                // handle parsing errors ...
            }
        }

        [Option(Description = "The subject")]
        public string Subject { get; }

        private void OnExecute()
        {
            var subject = Subject ?? "world";
            Console.WriteLine($"Hello {subject}!");
        }
    }
}
