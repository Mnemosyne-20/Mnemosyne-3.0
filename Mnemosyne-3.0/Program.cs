using ArchiveApi;
using ArchiveApi.Interfaces;
using Mnemosyne3.BotState;
using Mnemosyne3.Commenting;
using Mnemosyne3.Configuration;
using Mnemosyne3.UserData;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
namespace Mnemosyne3
{
    internal class InternalLogger
    {
        static readonly string FileName;
        static InternalLogger()
        {
            FileName = "Failures.txt";
            Directory.CreateDirectory("./Errors");
        }
        public static void Log(Exception e) => EnhancedLog("", e);
        public static void EnhancedLog(string message, Exception e) => File.AppendAllText("./Errors/" + FileName, $"{message}{e}{Environment.NewLine}");
        public static void Log(string error) => File.AppendAllText("./Errors/" + FileName, error + Environment.NewLine);
    }
    public class Program
    {
        #region static values
        public readonly static string[] ArchiveBots = new string[]
        {
            "mnemosyne-0001",
            "mnemosyne-0002",// I've seen you!
            "SpootsTestBot", // hey I know you!
            "Mentioned_Videos",
            "AutoModerator",
            "TotesMessenger",
            "TweetPoster",
            "RemindMeBot",
            "thelinkfixerbot",
            "gifv-bot",
            "autourbanbot",
            "deepsalter-001",
            "GoodBot_BadBot",
            "PORTMANTEAU-BOT",
            "GoodBot_BadBot_Karma",
            "MTGCardFetcher"
        };
        /// <summary>
        /// Iterates each "thing" you make, subreddit is required for a few of them
        /// </summary>
        /// <param name="reddit"></param>
        /// <param name="state"></param>
        /// <param name="subbreddit"></param>
        public delegate void IterateThing(Reddit reddit, IBotState state, ArchiveSubreddit subbreddit);
        public delegate void IterateSeparateConfigThing(Reddit reddit, IBotState state, ArchiveSubreddit[] subreddits, Config config);
        public delegate void IterateSubredditThing(Reddit reddit, IBotState state, ArchiveSubreddit subreddit, Config config);
        public static IterateSubredditThing IteratePost;
        public static IterateSubredditThing IterateComment;
        public static IterateThing IterateMessage;
        public static IterateSeparateConfigThing Iterate24Hours;
        /// <summary>
        /// This is intentional to be this way, it's so that the editor can get the headers easily
        /// </summary>
        public static readonly string[] Headers = new string[] { "Archives for this post:\n\n", "Archive for this post:\n\n", "Archives for the links in comments:\n\n", "----\nI am Mnemosyne 3.0, {0} ^^^^/r/botsrights ^^^^[Contribute](https://github.com/Mnemosyne-20/Mnemosyne-3.0) ^^^^message ^^^^me ^^^^suggestions ^^^^at ^^^^any ^^^^time ^^^^Opt ^^^^out ^^^^of ^^^^tracking ^^^^by ^^^^messaging ^^^^me ^^^^\"Opt ^^^^Out\" ^^^^at ^^^^any ^^^^time", "Archives after 24 hours:\n\n" };
        /// <summary>
        /// These three being separate is important because it is used for data tracking
        /// </summary>
        public readonly static Regex exclusions = new Regex(@"(facebook\.com|giphy\.com|streamable\.com|www\.gobrickindustry\.us|ushmm\.org|gyazo\.com|sli\.mg|imgur\.com|reddit\.com/message|wiki/rules|politics_feedback_results_and_where_it_goes_from|urbandictionary\.com)");
        public readonly static Regex YoutubeRegex = new Regex(@"(https?://youtu\.be(?<url>/[a-zA-Z0-9])?|https?://www\.youtube\.com/(?<url>watch\?v=[a-zA-Z0-9]+)?($|.+))");
        public readonly static Regex providers = new Regex(@"(web-beta.archive.org|archive\.is|archive\.fo|archive\.org|archive\.today|archive\.ph|archive\.md|megalodon\.jp|web\.archive\.org|webcache\.googleusercontent\.com|archive\.li)");
        public readonly static Regex ImageRegex = new Regex(@"(\.gif|\.jpg|\.png|\.pdf|\.webm|\.mp4|\.jpeg)$");
        private readonly static Regex[] allRegex = new Regex[] { exclusions, YoutubeRegex, providers, ImageRegex };
        #region Locks
        static object LockConfigObject = new object();
        static object LockArchiveSubredditsObject = new object();
        #endregion
        #endregion
        #region Local Values
        Reddit reddit;
        public event EventHandler<ConfigEventArgs> UpdatedConfig;
        public event EventHandler<ArchiveSubredditEventArgs> UpdatedArchiveSubreddits;
        Config _config;
        public Config Config
        {
            get
            {
                return _config;
            }
            set
            {
                lock (LockConfigObject)
                {
                    _config = value;
                }
                UpdatedConfig?.Invoke(this, new ConfigEventArgs(_config));
            }
        }
        ArchiveSubreddit[] _archiveSubreddits;
        public ArchiveSubreddit[] ArchiveSubreddits
        {
            get
            {
                return _archiveSubreddits;
            }
            set
            {
                lock (LockArchiveSubredditsObject)
                {
                    _archiveSubreddits = value;
                }
                UpdatedArchiveSubreddits?.Invoke(this, new ArchiveSubredditEventArgs(_archiveSubreddits));
            }
        }
        #endregion
        public static void GetHelp()
        {
            Console.WriteLine("Mnemosyne - 2.1 by chugga_fan");
            Console.WriteLine("Currently no supported command line options, but future options will be:");
            Console.WriteLine("\t--server | -s\tWill be used to start a web hosted version, with an ASP.NET host");
        }
        static void Main(string[] args)
        {
            Console.Title = "Mnemosyne-2.1 by chugga_fan";
            foreach (string s in args)
            {
                switch (s)
                {
                    case "--server":
                    case "-s":
                        break;
                    case "--help":
                    case "-h":
                    case "-?":
                        GetHelp();
                        return;
                    default:
                        break;
                }
            }
            _ = new Program();
        }
        public static ArchiveSubreddit[] InitializeArchiveSubreddits(Reddit reddit, Config config)
        {
            ArchiveSubreddit[] ArchiveSubreddits = new ArchiveSubreddit[config.Subreddits.Length];
            for (int i = 0; i < config.Subreddits.Length; i++)
            {
                ArchiveSubreddits[i] = reddit.GetArchiveSubreddit(config.Subreddits[i]);
            }
            return ArchiveSubreddits;
        }
        public Program()
        {
            Console.Clear();
            lock (LockConfigObject)
            {
                Config = !File.Exists("./Data/Settings.json") ? CreateNewConfig() : Config.GetConfig();
            }
            using (IBotState botstate = Config.SQLite ? (IBotState)new SqliteBotState() : new FlatBotState())
            {
                // create another filter so archive.is is also filtered
                new ArchiveService(DefaultServices.ArchiveIs).CreateNewService();
                WebAgent agent = null;
#pragma warning disable CS0618 // Type or member is obsolete
                lock (LockConfigObject)
                {
                    if (Config.UseOAuth)
                    {
                        agent = new BotWebAgent(Config.UserName, Config.Password, Config.OAuthClientId, Config.OAuthSecret, Config.RedirectURI);
                    }
                    else
                    {
                        Console.WriteLine("Error: redditsharp no longer allows non-oauth connections in updated versions, use oauth");
                        Environment.Exit(1);
                    }
                    reddit = new Reddit(agent);
                }
                reddit.InitOrUpdateUserAsync().Wait();
                UpdatedConfig += (sender, e) => { ArchiveSubreddits = InitializeArchiveSubreddits(reddit, e.Config); };
                UpdatedArchiveSubreddits += (sender, e) => { Console.Title = "Updated Archive Subreddits"; };
                lock (LockConfigObject)
                {
                    ArchiveSubreddits = InitializeArchiveSubreddits(reddit, Config);
                }
                IteratePost = IteratePosts;
                IterateComment = IterateComments;
                IterateMessage = IterateMessages;
                //Iterate24Hours = Iterate24HourArchive; // currently neutered so that it just does regular 24 hour passes
                _ = new RedditUserProfileSqlite();
                if (File.Exists("./Data/Users.json")) // Forces going to sqlite for user profiles because it's THAT MUCH BETTER
                {
                    RedditUserProfileSqlite.TransferProfilesToSqlite(RedditUserProfile.Users);
                    File.Delete("./Data/Users.json");
                }
#pragma warning restore CS0618 // Type or member is obsolete
                lock (LockConfigObject)
                {
                    if (botstate is FlatBotState)
                    {
                        Console.WriteLine("Beginning conversion to sqlite");
                        using (IBotState botstate2 = new SqliteBotState(botstate as FlatBotState))
                        {
                            // Intentional, create and dispose
                        }
                        Config.SQLite = true;
                        Config.DumpConfig();
                        Console.WriteLine("Done with SQLite conversion, please re-run this program and you will automatically be using sqlite");
                        return;
                    }

                }
                IArchiveService service = new ArchiveService(Config.ArchiveService).CreateNewService();
                #region ServiceCreation
                new ArchiveService(DefaultServices.ArchiveIs).CreateNewService();
                new ArchiveService(DefaultServices.ArchiveLi).CreateNewService();
                new ArchiveService(DefaultServices.ArchivePh).CreateNewService();
                new ArchiveService(DefaultServices.ArchiveVn).CreateNewService();
                new ArchiveService(DefaultServices.ArchiveMd).CreateNewService();
                new ArchiveService(DefaultServices.ArchiveToday).CreateNewService();
                new ArchiveService(DefaultServices.ArchiveFo).CreateNewService();
                #endregion
                MainLoop(reddit, botstate);
            }
        }
        public static async Task<bool> HasMessages(Reddit reddit)
        {
            return await reddit.User.GetUnreadMessages().AnyAsync();
        }
        public void MainLoop(Reddit reddit, IBotState botstate)
        {
            while (true) // main loop, calls delegates that move thrugh every subreddit allowed iteratively
            {
                try
                {
                    lock (LockArchiveSubredditsObject)
                    {
                        foreach (ArchiveSubreddit sub in ArchiveSubreddits) // Iterates allowed subreddits
                        {
                            IteratePost?.Invoke(reddit, botstate, sub, Config);
                            IterateComment?.Invoke(reddit, botstate, sub, Config);
                            IterateMessage?.Invoke(reddit, botstate, sub);
                        }
                        Iterate24Hours?.Invoke(reddit, botstate, ArchiveSubreddits, Config);
                    }
                    Console.Title = $"Sleeping, New messages: {HasMessages(reddit).Result}";
                }
                catch (WebException e) when (e.Message.Contains("(404)") || e.Message.Contains("Cannot resolve hostname") && (int)((HttpWebResponse)e.Response).StatusCode <= 500 && (int)((HttpWebResponse)e.Response).StatusCode >= 600)
                {
                    Console.WriteLine("Connect to the internet, Error: " + e.Message);
                }
                catch (Exception e) when (e is not NullReferenceException)
                {
                    if (e.Message.Contains("(502)") || e.Message.Contains("(503)") || e.Message.Contains("The remote name could not be resolved"))
                    {
                        continue;
                    }
                    InternalLogger.Log(e);
                    Console.WriteLine($"Caught an exception of type {e.GetType()} output is in ./Errors/Failures.txt");
                }
                Thread.Sleep(1000); // sleeps for one second to help with the reddit calls
            }
        }
        public static Config CreateNewConfig()
        {
            Console.WriteLine("Would you like to create a new account? (Yes/No)");
            string Username, Password;
            if (Console.ReadLine().ToLower()[0] == 'y')
            {
                Reddit red = new Reddit();
                Console.WriteLine("Input a username");
                Username = Console.ReadLine();
                Console.WriteLine("Input a password");
                Password = Console.ReadLine();
                _ = red.RegisterAccountAsync(Username, Password).Result;
            }
            Console.WriteLine("What is your username?");
            Username = Console.ReadLine();
            Console.WriteLine("What is your password? note: required and is stored in plaintext, suggest you use a secure system");
            Password = Console.ReadLine();
            Console.WriteLine("How many subreddits are you watching?");
            int len;
            while (!int.TryParse(Console.ReadLine(), out len))
                Console.WriteLine("Please input a valid integer.");
            ArchiveSubredditJson[] Subs = new ArchiveSubredditJson[len];
            for (int i = 0; i < len; i++)
            {
                Console.WriteLine("What is the name of the subreddit?");
                string name = Console.ReadLine();
                Console.WriteLine("Would you like to archive posts? (Yes/No)");
                bool ArcPost = Console.ReadLine().ToLower()[0] == 'y';
                bool Arc24Hours = false;
                if (ArcPost)
                {
                    Console.WriteLine("Would you like to archive posts again after 24 hours? (Yes/No)");
                    Arc24Hours = Console.ReadLine().ToLower()[0] == 'y';
                }
                Console.WriteLine("Would you like to archive links in comments? (Yes/No)");
                bool ArcComments = Console.ReadLine().ToLower()[0] == 'y';
                ArchiveSubredditJson arcSubJson = new ArchiveSubredditJson()
                {
                    ArchiveCommentLinks = ArcComments,
                    ArchivePost = ArcPost,
                    Name = name,
                    ArchiveWebsite = "archive.fo",
                    ArchiveAfter24Hours = Arc24Hours
                };
                Subs[i] = arcSubJson;
            }

            Console.WriteLine("Get an OAuth client ID and Secret");
            Console.WriteLine("What is your clientID?");
            string ClientID = Console.ReadLine();
            Console.WriteLine("What is your client secret?");
            string ClientSecret = Console.ReadLine();
            Console.WriteLine("Do you want to archive post links? (Yes/No)");
            bool ArchiveLinks = Console.ReadLine().ToLower()[0] == 'y';
            Console.WriteLine("To add flavortext, you must manually add it in as an array in the ./Data/Settings.json file");
            Console.Title = "Sleeping for 10000 ms";
            Thread.Sleep(10000);
            return new Config(Username, Subs, Password, ClientSecret, ClientID, ArchiveLinks);
        }
        #region IterateThings
        public static void IterateMessages(Reddit reddit, IBotState state, ArchiveSubreddit subreddit)
        {
            if (reddit == null || state == null || subreddit == null)
            {
                throw new ArgumentNullException(reddit == null ? nameof(reddit) : state == null ? nameof(state) : nameof(subreddit));
            }
            foreach (var message in reddit.User.GetPrivateMessages().Take(25).ToEnumerable())
            {
                if (!message.Unread)
                {
                    break;
                }
                switch (message.Body.ToLower())
                {
                    case "opt out":
                        Console.WriteLine($"User {message.AuthorName} has opted out.");
                        new RedditUserProfileSqlite(reddit.GetUserAsync(message.AuthorName).Result).OptedOut = true;
                        message.SetAsReadAsync().RunSynchronously();
                        break;
                    case "opt in":
                        Console.WriteLine($"User {message.AuthorName} has opted in");
                        new RedditUserProfileSqlite(reddit.GetUserAsync(message.AuthorName).Result).OptedOut = false;
                        message.SetAsReadAsync().RunSynchronously();
                        break;
                }
            }
        }
        public static void IteratePosts(Reddit reddit, IBotState state, ArchiveSubreddit subreddit, Config config)
        {
            if (reddit == null || state == null || subreddit == null || config == null)
            {
                throw new ArgumentNullException(reddit == null ? nameof(reddit) : state == null ? nameof(state) : config == null ? nameof(config) : nameof(subreddit));
            }
            Console.Title = $"Finding posts in {subreddit.Name} New messages: {HasMessages(reddit).Result}";
            foreach (var post in subreddit.New.Take(25).ToEnumerable())
            {
                if (!state.DoesCommentExist(post.Id) && !state.HasPostBeenChecked(post.Id))
                {
                    List<string> Links = new List<string>();
                    if (post.IsSelfPost && !string.IsNullOrEmpty(post.SelfTextHtml))
                    {
                        Links = RegularExpressions.FindLinks(post.SelfTextHtml);
                    }
                    if (Links.Count == 0 && !subreddit.ArchivePost)
                    {
                        state.AddCheckedPost(post.Id);
                        continue;
                    }
                    if (Links.Count > 0)
                    {
                        foreach (string s in Links)
                        {
                            Console.WriteLine($"Found {s} in post {post.Id}");
                        }
                    }
                    List<ArchiveLink> ArchivedLinks = ArchiveLinks.ArchivePostLinks(Links, allRegex, reddit.GetUserAsync(post.AuthorName).Result, subreddit.SubredditArchiveService);
                    lock (LockConfigObject)
                    {
                        PostArchives.ArchivePostLinks(subreddit, config, state, post, ArchivedLinks);
                    }
                    Console.WriteLine($"Added post: {post.Id} in subreddit {post.SubredditName}");
                    if (!subreddit.ArchiveAfter24Hours)
                    {
                        Console.WriteLine("Checked post: " + post.Id);
                        state.Archive24Hours(post.Id);
                    }
                }
            }
        }
        public static void IterateComments(Reddit reddit, IBotState state, ArchiveSubreddit subreddit, Config config)
        {
            if (reddit == null || state == null || subreddit == null || config == null)
            {
                throw new ArgumentNullException(reddit == null ? nameof(reddit) : state == null ? nameof(state) : config == null ? nameof(config) : nameof(subreddit));
            }
            if (!subreddit.ArchiveCommentLinks)
            {
                return;
            }
            Console.Title = $"Finding comments in {subreddit.Name} New messages: {HasMessages(reddit).Result}";
            foreach (var comment in subreddit.Comments.Take(25).ToEnumerable().Where(a => !ArchiveBots.Contains(a.AuthorName)))
            {
                List<string> Links = RegularExpressions.FindLinks(comment.BodyHtml);
                if (state.HasCommentBeenChecked(comment.Id) || Links.Count == 0)
                {
                    continue;
                }
                foreach (string s in Links)
                {
                    Console.WriteLine($"Found {s} in comment {comment.Id}");
                }
                List<ArchiveLink> ArchivedLinks = ArchiveLinks.ArchivePostLinks(Links, allRegex, reddit.GetUserAsync(comment.AuthorName).Result, subreddit.SubredditArchiveService);
                lock (LockConfigObject)
                {
                    PostArchives.ArchiveCommentLinks(config, state, reddit, comment, ArchivedLinks);
                }
                state.AddCheckedComment(comment.Id);
            }
        }
        public static void Iterate24HourArchive(Reddit reddit, IBotState state, ArchiveSubreddit[] subreddits, Config config)
        {
            if (reddit == null || state == null || subreddits == null || config == null)
            {
                throw new ArgumentNullException(reddit == null ? nameof(reddit) : state == null ? nameof(state) : config == null ? nameof(config) : nameof(subreddits));
            }
            Console.Title = $"Archiving posts after 24 hours";
            // Shut the fuck up about the name, I know it's stupid long, but it exists for literally only this, so can it
            var compararer = new ArchiveSubredditEqualityCompararer();
            foreach (var postId in state.GetNon24HourArchivedPosts())
            {
                List<ArchiveLink> ArchivedLinks = new List<ArchiveLink>();
                List<string> Links = new List<string>();
                Post post = (Post)reddit.GetThingByFullnameAsync($"t3_{Regex.Replace(postId, "^(t[0-6]_)", "")}").Result;
                if (DateTime.UtcNow.Subtract(new TimeSpan(TimeSpan.TicksPerDay)) < post.CreatedUTC)
                {
                    continue;
                }
                Console.WriteLine("Got past the 24 hours marker");
                ArchiveSubreddit sub = subreddits.First((a) => a.Name == post.SubredditName);
                if (!sub.ArchiveAfter24Hours)
                {
                    state.Archive24Hours(post.Id);
                    continue;
                }
                if (post.IsSelfPost && !string.IsNullOrEmpty(post.SelfTextHtml))
                {
                    Links = RegularExpressions.FindLinks(post.SelfTextHtml);
                }
                if (Links.Count == 0 && !sub.ArchivePost)
                {
                    state.Archive24Hours(post.Id);
                    continue;
                }
                else if (Links.Count > 0)
                {
                    foreach (string s in Links)
                    {
                        Console.WriteLine($"Found {s} in post {post.Id} when rearchiving after 24 hours");
                    }
                }
#if POSTTEST
                ArchivedLinks = ArchiveLinks.ArchivePostLinks(Links, allRegex, post.Author);
                lock (LockConfigObject)
                {
                    PostArchives.ArchivePostLinks24Hours(sub, reddit, config, state, post, ArchivedLinks);
                }
#endif
                Console.WriteLine("Sucessfully made a 24 hour archive comment");
                state.Archive24Hours(post.Id);
            }
        }
        #endregion
    }
}