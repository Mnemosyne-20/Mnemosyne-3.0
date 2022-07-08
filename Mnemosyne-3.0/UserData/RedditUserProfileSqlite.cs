#if !ARM
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
namespace Mnemosyne3.UserData
{
    public class RedditUserProfileSqlite
    {
        static SqliteCommand
            SqliteSetUnarchived,
            SqliteSetArchived,
            SqliteSetExcluded,
            SqliteSetImage,
            SqliteGetImage,
            SqliteAddUser,
            SqliteGetArchived, 
            SqliteGetUnarchived, 
            SqliteGetExcluded, 
            SqliteGetOptOut, 
            SqliteSetOptOut, 
            SqliteGetUserExists, 
            SqliteAvgExcluded, 
            SqliteAvgImage, 
            SqliteAvgArchived, 
            SqliteAvgUnarchived;
        public static SqliteConnection Connection { get; private set; }
        static bool Initialized = false;
#pragma warning disable CS0618 // Type or member is obsolete
        public static void TransferProfilesToSqlite(Dictionary<string, RedditUserProfile> dict)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            CheckInitialized();
            var optedOut = from a in dict.AsParallel() where a.Value.OptedOut select a; // parallel because the dictionary can be absolutely enormous depending on length of runtime
            foreach (var user in optedOut)
            {
                try
                {
                    new RedditUserProfileSqlite(new Reddit().GetUserAsync(user.Key).Result).OptedOut = true;
                }
                catch (System.Net.WebException e) when (e.Message.Contains("(404)"))
                {
                    Console.WriteLine($"User {user.Key} no longer exists");
                }
            }
        }
        static void CheckInitialized()
        {
            if (!Initialized)
                throw new InvalidOperationException("You must initialize using the string based constructor first, then you may use the class later on");
        }
        public static bool UserExists(string User)
        {
            CheckInitialized();
            SqliteGetUserExists.Parameters["@Name"].Value = User;
            return Convert.ToBoolean(SqliteGetUserExists.ExecuteScalar());
        }
        public static bool UserExists(RedditUser User) => UserExists(User.Name);
        public bool OptedOut
        {
            get
            {
                SqliteGetOptOut.Parameters["@Name"].Value = User;
                return Convert.ToBoolean(SqliteGetOptOut.ExecuteScalar());
            }
            set
            {
                SqliteSetOptOut.Parameters["@Name"].Value = User;
                SqliteSetOptOut.Parameters["@OptedOut"].Value = value ? 1 : 0;
                SqliteSetOptOut.ExecuteNonQuery();
            }
        }
        public static float AverageImage
        {
            get
            {
                CheckInitialized();
                return Convert.ToSingle(SqliteAvgImage.ExecuteScalar());
            }
        }
        public int Image
        {
            get
            {
                SqliteGetImage.Parameters["@Name"].Value = User;
                return Convert.ToInt32(SqliteGetImage.ExecuteScalar());
            }
            set
            {
                SqliteSetImage.Parameters["@Name"].Value = User;
                SqliteSetImage.Parameters["@ImageUrls"].Value = value;
                SqliteSetImage.ExecuteNonQuery();
            }
        }
        public float AverageUnarchived
        {
            get
            {
                CheckInitialized();
                return Convert.ToSingle(SqliteAvgUnarchived.ExecuteScalar());
            }
        }
        public int Unarchived
        {
            get
            {
                SqliteGetUnarchived.Parameters["@Name"].Value = User;
                return Convert.ToInt32(SqliteGetUnarchived.ExecuteScalar());
            }
            set
            {
                SqliteSetUnarchived.Parameters["@Name"].Value = User;
                SqliteSetUnarchived.Parameters["@UnarchivedUrls"].Value = value;
                SqliteSetUnarchived.ExecuteNonQuery();
            }
        }
        public static float AverageArchived
        {
            get
            {
                CheckInitialized();
                return Convert.ToSingle(SqliteAvgArchived.ExecuteScalar());
            }
        }
        public int Archived
        {
            get
            {
                SqliteGetArchived.Parameters["@Name"].Value = User;
                return Convert.ToInt32(SqliteGetArchived.ExecuteScalar());
            }
            set
            {
                SqliteSetArchived.Parameters["@Name"].Value = User;
                SqliteSetArchived.Parameters["@ArchivedUrls"].Value = value;
                SqliteSetArchived.ExecuteNonQuery();
            }
        }
        public static float AverageExcluded
        {
            get
            {
                CheckInitialized();
                return Convert.ToSingle(SqliteAvgExcluded.ExecuteScalar());
            }
        }
        public int Excluded
        {
            get
            {
                SqliteGetExcluded.Parameters["@Name"].Value = User;
                return Convert.ToInt32(SqliteGetExcluded.ExecuteScalar());
            }
            set
            {
                SqliteSetExcluded.Parameters["@Name"].Value = User;
                SqliteSetExcluded.Parameters["@ExcludedUrls"].Value = value;
                SqliteSetExcluded.ExecuteNonQuery();
            }
        }

        readonly string User;
        public void AddUrlUsed(string url)
        {
            if (OptedOut)
            {
                return;
            }
            if (Program.exclusions.IsMatch(url.ToString()) || Program.YoutubeRegex.IsMatch(url))
            {
                Excluded++;
                return;
            }
            if (Program.providers.IsMatch(url.ToString()))
            {
                Archived++;
            }
            else
            {
                Unarchived++;
            }
            if (Program.ImageRegex.IsMatch(url.ToString()) || Program.ImageRegex.IsMatch(new Uri(url).AbsolutePath))
            {
                Image++;
            }
        }
        static void InitDbTable()
        {
            using (SqliteCommand cmd = new SqliteCommand("create table if not exists Users (Name text unique, UnarchivedUrls integer, ImageUrls integer, ArchivedUrls integer, ExcludedUrls integer, OptedOut integer)", Connection))
                cmd.ExecuteNonQuery();
        }
        static void InitDbCommands()
        {
            SqliteParameter UserNameParam = new SqliteParameter("@Name", DbType.String);

            SqliteGetUserExists = new SqliteCommand("select count(*) from Users where Name = @Name", Connection);
            SqliteGetUserExists.Parameters.Add(UserNameParam);

            SqliteAddUser = new SqliteCommand("insert or abort into Users(Name, UnarchivedUrls, ImageUrls, ArchivedUrls, ExcludedUrls, OptedOut) values(@Name, 0, 0, 0, 0, 0)", Connection);
            SqliteAddUser.Parameters.Add(UserNameParam);

            SqliteGetUnarchived = new SqliteCommand("select UnarchivedUrls from Users where Name = @Name", Connection);
            SqliteGetUnarchived.Parameters.Add(UserNameParam);

            SqliteGetArchived = new SqliteCommand("select ArchivedUrls from Users where Name = @Name", Connection);
            SqliteGetArchived.Parameters.Add(UserNameParam);

            SqliteGetOptOut = new SqliteCommand("select OptedOut from Users where Name = @Name", Connection);
            SqliteGetOptOut.Parameters.Add(UserNameParam);

            SqliteGetExcluded = new SqliteCommand("select ExcludedUrls from Users where Name = @Name", Connection);
            SqliteGetExcluded.Parameters.Add(UserNameParam);

            SqliteSetArchived = new SqliteCommand("update Users set ArchivedUrls = @ArchivedUrls where Name = @Name", Connection);
            SqliteSetArchived.Parameters.Add(new SqliteParameter("@ArchivedUrls", DbType.Int32));
            SqliteSetArchived.Parameters.Add(UserNameParam);

            SqliteSetExcluded = new SqliteCommand("update Users set ExcludedUrls = @ExcludedUrls where Name = @Name", Connection);
            SqliteSetExcluded.Parameters.Add(new SqliteParameter("@ExcludedUrls", DbType.Int32));
            SqliteSetExcluded.Parameters.Add(UserNameParam);

            SqliteSetUnarchived = new SqliteCommand("update Users set UnarchivedUrls = @UnarchivedUrls where Name = @Name", Connection);
            SqliteSetUnarchived.Parameters.Add(new SqliteParameter("@UnarchivedUrls", DbType.Int32));
            SqliteSetUnarchived.Parameters.Add(UserNameParam);

            SqliteSetOptOut = new SqliteCommand("update Users set OptedOut = @OptedOut where Name = @Name", Connection);
            SqliteSetOptOut.Parameters.Add(new SqliteParameter("@OptedOut", DbType.Int32));
            SqliteSetOptOut.Parameters.Add(UserNameParam);

            SqliteSetImage = new SqliteCommand("update Users set ImageUrls = @ImageUrls where Name = @Name", Connection);
            SqliteSetImage.Parameters.Add(new SqliteParameter("@ImageUrls", DbType.Int32));
            SqliteSetImage.Parameters.Add(UserNameParam);

            SqliteGetImage = new SqliteCommand("select ImageUrls from Users where Name = @Name", Connection);
            SqliteGetImage.Parameters.Add(UserNameParam);

            SqliteAvgArchived = new SqliteCommand("select avg(ArchivedUrls) from Users");

            SqliteAvgExcluded = new SqliteCommand("select avg(ExcludedUrls) from Users");

            SqliteAvgImage = new SqliteCommand("select avg(ImageUrls) from Users");

            SqliteAvgUnarchived = new SqliteCommand("select avg(UnarchivedUrls) from Users");
        }
        public RedditUserProfileSqlite(string fileName = "redditusers.sqlite")
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", $"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/')}/Data/");
            Connection = new SqliteConnection($"Data Source=|DataDirectory|{fileName};");
            Connection.Open();
            InitDbTable();
            InitDbCommands();
            Initialized = true;
        }
        public RedditUserProfileSqlite(RedditUser user)
        {
            CheckInitialized();
            User = user.Name;
            if (!UserExists(user))
            {
                SqliteAddUser.Parameters["@Name"].Value = user.Name;
                SqliteAddUser.ExecuteNonQuery();
            }
        }
    }
}
#endif