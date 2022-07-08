using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Mnemosyne3.BotState
{
    public class SqliteBotState : IBotState
    {
        SqliteConnection dbConnection;
        SqliteCommand SQLCmd_AddBotComment,
            SQLCmd_AddCheckedComment,
            SQLCmd_DoesBotCommentExist,
            SQLCmd_GetBotComment,
            SQLCmd_HasCommentBeenChecked,
            SQLCmd_HasPostBeenChecked,
            SQLCmd_AddCheckedPost,
            SQLCmd_UpdateBotComment,
            SQLCmd_Update24HourArchive,
            SQLCmd_Is24HourArchived,
            SQLCmd_GetNon24HourArchived,
            SQLCmd_RemoveBotPost;
        public SqliteBotState(FlatBotState flatBotState, string filename = "botstate.sqlite")
        {
            if (flatBotState == null)
            {
                throw new ArgumentNullException(nameof(flatBotState));
            }
            AppDomain.CurrentDomain.SetData("DataDirectory", $"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/')}/Data/");
            dbConnection = new SqliteConnection($"Data Source=|DataDirectory|{filename};");
            dbConnection.Open();
            InitializeDatabase();
            InitializeCommands();
            Console.WriteLine("Beginning to add all checked posts");
            foreach(var thing in flatBotState.GetAllCheckedPosts())
            {
                this.AddCheckedPost(thing);
            }
            Console.WriteLine("Done adding checked posts");
            Console.WriteLine("Adding 24 hour archive information");
            foreach(var thing in flatBotState.GetAllPosts24Hours())
            {
                if(thing.Value)
                {
                    this.Archive24Hours(thing.Key);
                }
            }
            Console.WriteLine("Done adding 24 hour archive information");
            Console.WriteLine("Getting all bot comments");
            foreach(var thing in flatBotState.GetAllBotComments())
            {
                this.AddBotComment(thing.Key, thing.Value);
            }
            Console.WriteLine("Done getting all bot comments");
            Console.WriteLine("Getting all checked comments");
            foreach(var thing in flatBotState.GetAllCheckedComments())
            {
                this.AddCheckedComment(thing);
            }
            Console.WriteLine("Done getting all checked comments");
        }
        public SqliteBotState(string filename = "botstate.sqlite")
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", $"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/')}/Data/");
            dbConnection = new SqliteConnection($"Data Source=|DataDirectory|{filename};foreign keys=True;");
            dbConnection.Open();
            InitializeDatabase();
            InitializeCommands();
        }
        public bool DoesCommentExist(string postID)
        {
            SQLCmd_DoesBotCommentExist.Parameters["@postID"].Value = postID;
            return Convert.ToBoolean(SQLCmd_DoesBotCommentExist.ExecuteScalar());
        }
        public void AddCheckedComment(string commentID)
        {
            try
            {
                SQLCmd_AddCheckedComment.Parameters["@commentID"].Value = commentID;
                SQLCmd_AddCheckedComment.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException($"The comment {commentID} already exists in database");
            }
        }
        public bool HasCommentBeenChecked(string commentID)
        {
            SQLCmd_HasCommentBeenChecked.Parameters["@commentID"].Value = commentID;
            return Convert.ToBoolean(SQLCmd_HasCommentBeenChecked.ExecuteScalar());
        }
        void InitializeDatabase()
        {
            string query = "create table if not exists posts (postID TEXT NOT NULL UNIQUE, reArchived INTEGER NOT NULL, PRIMARY KEY(postID))";
            using (SqliteCommand cmd = new SqliteCommand(query, dbConnection))
            {
                cmd.ExecuteNonQuery();
            }
            query = "create table if not exists replies (postID TEXT NOT NULL UNIQUE, botReplyID text, FOREIGN KEY (postID) REFERENCES posts(postID) ON DELETE CASCADE)";
            using (SqliteCommand cmd = new SqliteCommand(query, dbConnection))
            {
                cmd.ExecuteNonQuery();
            }
            query = "create table if not exists comments (commentID text unique)"; // yes this is a table with one column and eventually along with the reply table won't even be needed at all
            using (SqliteCommand cmd = new SqliteCommand(query, dbConnection))
            {
                cmd.ExecuteNonQuery();
            }
            query = "create table if not exists archives (originalURL text unique, numArchives integer)";
            using (SqliteCommand cmd = new SqliteCommand(query, dbConnection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        void InitializeCommands()
        {
            var PostParam = new SqliteParameter("@postID", DbType.String);
            var BotReplyParam = new SqliteParameter("@botReplyID", DbType.String);
            var CommentIdParam = new SqliteParameter("@commentID", DbType.String);
            SQLCmd_AddBotComment = new SqliteCommand("insert or abort into replies(postID, botReplyID) values(@postID, @botReplyID)", dbConnection);
            SQLCmd_AddBotComment.Parameters.Add(PostParam);
            SQLCmd_AddBotComment.Parameters.Add(BotReplyParam);

            SQLCmd_AddCheckedComment = new SqliteCommand("insert or abort into comments (commentID) values (@commentID)", dbConnection);
            SQLCmd_AddCheckedComment.Parameters.Add(CommentIdParam);

            SQLCmd_AddCheckedPost = new SqliteCommand("insert or abort into posts (postID, reArchived) values (@postID, 0)", dbConnection);
            SQLCmd_AddCheckedPost.Parameters.Add(PostParam);

            SQLCmd_DoesBotCommentExist = new SqliteCommand("select count(*) from replies where postID = @postID", dbConnection);
            SQLCmd_DoesBotCommentExist.Parameters.Add(PostParam);

            SQLCmd_GetBotComment = new SqliteCommand("select botReplyID from replies where postID = @postID", dbConnection);
            SQLCmd_GetBotComment.Parameters.Add(PostParam);

            SQLCmd_HasCommentBeenChecked = new SqliteCommand("select count(commentID) from comments where commentID = @commentID", dbConnection);
            SQLCmd_HasCommentBeenChecked.Parameters.Add(CommentIdParam);

            SQLCmd_HasPostBeenChecked = new SqliteCommand("select count(postID) from posts where postID = @postID", dbConnection);
            SQLCmd_HasPostBeenChecked.Parameters.Add(PostParam);

            SQLCmd_UpdateBotComment = new SqliteCommand("update replies set botReplyID = @botReplyID where postID = @postID", dbConnection);
            SQLCmd_UpdateBotComment.Parameters.Add(BotReplyParam);
            SQLCmd_UpdateBotComment.Parameters.Add(PostParam);

            SQLCmd_Update24HourArchive = new SqliteCommand("update posts set reArchived = 1 where postID = @postID", dbConnection);
            SQLCmd_Update24HourArchive.Parameters.Add(PostParam);

            SQLCmd_Is24HourArchived = new SqliteCommand("select reArchived from posts where postID = @postID", dbConnection);
            SQLCmd_Is24HourArchived.Parameters.Add(PostParam);

            SQLCmd_GetNon24HourArchived = new SqliteCommand("select postID from posts where reArchived = 0", dbConnection);
            SQLCmd_RemoveBotPost = new SqliteCommand("DELETE FROM posts WHERE postID = @postID", dbConnection);
            SQLCmd_RemoveBotPost.Parameters.Add(PostParam);
        }
        public void AddBotComment(string postID, string commentID)
        {
            try
            {
                SQLCmd_AddBotComment.Parameters["@postID"].Value = postID;
                SQLCmd_AddBotComment.Parameters["@botReplyID"].Value = commentID;
                SQLCmd_AddBotComment.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException($"The post {postID} already exists in database or has not been checked before");
            }
        }

        public string GetCommentForPost(string postID)
        {
            SQLCmd_GetBotComment.Parameters["@postID"].Value = postID;
            string botReplyID = Convert.ToString(SQLCmd_GetBotComment.ExecuteScalar());
            if (string.IsNullOrWhiteSpace(botReplyID))
            {
                throw new InvalidOperationException($"Comment ID for post {postID} is null or empty");
            }
            return botReplyID;
        }

        public void UpdateBotComment(string postID, string commentID)
        {
            SQLCmd_UpdateBotComment.Parameters["@postID"].Value = postID;
            SQLCmd_UpdateBotComment.Parameters["@botReplyID"].Value = commentID;
            SQLCmd_UpdateBotComment.ExecuteNonQuery();
        }

        public void AddCheckedPost(string postId)
        {
            try
            {
                SQLCmd_AddCheckedPost.Parameters["@postID"].Value = postId;
                SQLCmd_AddCheckedPost.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException($"the post {postId} already exists in the database");
            }
        }

        public bool HasPostBeenChecked(string postId)
        {
            SQLCmd_HasPostBeenChecked.Parameters["@postID"].Value = postId;
            return Convert.ToBoolean(SQLCmd_HasPostBeenChecked.ExecuteScalar());
        }
        public bool Is24HourArchived(string postId)
        {
            SQLCmd_Is24HourArchived.Parameters["@postID"].Value = postId;
            return Convert.ToBoolean(SQLCmd_Is24HourArchived.ExecuteScalar());
        }

        public void Archive24Hours(string postId)
        {
            SQLCmd_Update24HourArchive.Parameters["@postID"].Value = postId;
            SQLCmd_Update24HourArchive.ExecuteNonQuery();
        }
        public string[] GetNon24HourArchivedPosts()
        {
            List<string> readerCounter = new List<string>();
            using (SqliteDataReader reader = SQLCmd_GetNon24HourArchived.ExecuteReader())
            {
                while (reader.Read())
                {
                    readerCounter.Add((string)reader["postID"]);
                }
            }
            return readerCounter.ToArray();
        }
        public void DeletePostChecked(string postID)
        {
            SQLCmd_RemoveBotPost.Parameters["@postID"].Value = postID;
            SQLCmd_RemoveBotPost.ExecuteNonQuery();
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SQLCmd_AddBotComment.Dispose();
                    SQLCmd_AddCheckedComment.Dispose();
                    SQLCmd_DoesBotCommentExist.Dispose();
                    SQLCmd_GetBotComment.Dispose();
                    SQLCmd_HasCommentBeenChecked.Dispose();
                    SQLCmd_HasPostBeenChecked.Dispose();
                    SQLCmd_AddCheckedPost.Dispose();
                    SQLCmd_UpdateBotComment.Dispose();
                    SQLCmd_Is24HourArchived.Dispose();
                    SQLCmd_Update24HourArchive.Dispose();
                    SQLCmd_GetNon24HourArchived.Dispose();
                    SQLCmd_RemoveBotPost.Dispose();
                    dbConnection.Dispose();
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqliteBotState() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}