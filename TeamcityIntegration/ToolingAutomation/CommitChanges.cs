using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using ToolingAutomation.DomainModel;

namespace ToolingAutomation
{
    public class CommitChanges
    {
        public string TeamCityServer { get; }
        public string BuildId { get; }
        public string UserName { get; }
        public string Password { get; }
        public string OutputFileLocation { get; }
        public string BuildVersion { get; }

        public CommitChanges(string teamCityServer, string buildId, string userName, string password,
                            string outputFileLocation, string buildVersion)
        {
            TeamCityServer = teamCityServer;
            BuildId = buildId;
            UserName = userName;
            Password = password;
            OutputFileLocation = outputFileLocation;
            BuildVersion = buildVersion;
        }

        public static void AddCommand(CommandLineApplication app)
        {
            CommandLineApplication changes = app.Command("changes", config =>
            {
                config.Description = "Get TeamCity Changes for a given build Id";
                //var server = config.Argument("server", "TeamCity Url", false);
                //var buildId = config.Argument("buildId", "TeamCity Build Id", false);
                //config.HelpOption("-? | -h | --help"); //show help on --help

                CommandArgument arguments = config.Argument(
                    "arguments",
                    "Enter the TeamCity Url and Build Id.",
                    multipleValues: true);

                CommandOption server = config.Option(
                    "-$|-s |--server <url>",
                    "The TeamCity Url",
                    CommandOptionType.SingleValue);

                CommandOption buildId = config.Option(
                    "-$|-b |--buildId <Id>",
                    "The TeamCity Url",
                    CommandOptionType.SingleValue);

                CommandOption userName = config.Option(
                    "-$|-u |--username <Id>",
                    "The TeamCity UserName",
                    CommandOptionType.SingleValue);

                CommandOption password = config.Option(
                    "-$|-p |--password <Id>",
                    "The TeamCity Password",
                    CommandOptionType.SingleValue);

                CommandOption output = config.Option(
                    "-$|-o |--output <Id>",
                    "The output file location",
                    CommandOptionType.SingleValue);

                CommandOption buildVersion = config.Option(
                    "-$|-v |--version <Id>",
                    "The build version number",
                    CommandOptionType.SingleValue);

                config.OnExecute(async () =>
                {
                    Console.WriteLine("Changes Are Executing");
                    if (config.Options.All(a => a.HasValue()))
                    {
                        var commitChanges = new CommitChanges(server.Value(), buildId.Value(), userName.Value(), password.Value(),
                                                output.Value(), buildVersion.Value());
                        await commitChanges.HandleCommand();

                        return 0;
                    }

                    Console.WriteLine("parameters were missing!");
                    return 1;
                });
            });

            changes.Command("help", config =>
            {
                config.Description = "get help!";
                config.OnExecute(() =>
                {
                    changes.ShowHelp("changes");
                    return 1;
                });
            });
        }

        public async Task HandleCommand()
        {
            try
            {
                // http://teamcity.sogeti-techshare.com:8112

                HttpClient client = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes("admin:#meister-Sogeti!");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var stringTask = await client.GetStringAsync(String.Format("{0}/httpAuth/app/rest/changes?build=id:{1}", TeamCityServer, BuildId));

                XDocument changeDoc = XDocument.Parse(stringTask);
                if (changeDoc.Root != null)
                {
                    List<Change> changes = changeDoc.Root
                        .Elements("change")
                        .Select(x => new Change
                        {
                            Id = (string) x.Attribute("id")
                        })
                        .ToList();

                    if (changes.Any())
                    {
                        var changeDetails = await GetCommitMessages(changes);

                        var output = new CommitComments
                        {
                            Version = BuildVersion,
                            CommitDetails = changeDetails.Select(s => new CommitDetail
                            {
                                UserName = s.UserName,
                                Comment = s.Comment
                            }).ToList()
                        };

                        List<CommitComments> outputList = new List<CommitComments>();
                        if (File.Exists(OutputFileLocation))
                        {
                            outputList.AddRange(JsonConvert.DeserializeObject<List<CommitComments>>(File.ReadAllText(OutputFileLocation)));
                        }
                        outputList.Add(output);

                        IOrderedEnumerable<CommitComments> orderedEnumerable = outputList.OrderByDescending(comments => comments.Version);

                        string json = JsonConvert.SerializeObject(output, Formatting.Indented);
                        using (StreamWriter file = File.CreateText(OutputFileLocation))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(file, orderedEnumerable);
                        }
                    }
                }
                Console.WriteLine(stringTask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<List<ChangeDetail>> GetCommitMessages(List<Change> changes)
        {
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes("admin:#meister-Sogeti!");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            List<ChangeDetail> changeDetails = new List<ChangeDetail>();

            foreach (Change change in changes)
            {
                var response = await client.GetStringAsync(String.Format("{0}/httpAuth/app/rest/changes/id:{1}", TeamCityServer, change.Id));

                XDocument changeDoc = XDocument.Parse(response);
                if (changeDoc.Root != null)
                {
                        
                    var userName = changeDoc.Root?.Attribute("username")?.Value;
                    var comment = changeDoc.Root?.Element("comment")?.Value;

                    changeDetails.Add(new ChangeDetail
                    {
                        UserName = userName,
                        Comment = comment
                    });
                }
            }

            return changeDetails;
        }
    }
}
