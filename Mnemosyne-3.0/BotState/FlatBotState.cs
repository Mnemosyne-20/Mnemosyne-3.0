using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Mnemosyne3.BotState
{
    public class FlatBotState : IBotState
    {
        /// <summary>
        /// Returns a dictionary the exact same as the new format from the old format
        /// </summary>
        /// <param name="file">File to old format for the original bot</param>
        /// <returns>New dictionary</returns>
        static Dictionary<string, string> ReadReplyTrackingFile(string file)
        {
            Dictionary<string, string> replyDict = new Dictionary<string, string>();
            string[] elements = File.ReadAllText(file).Split(new char[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < elements.Length; i += 2)
            {
                string postID = elements[i];
                string botCommentID = elements[i + 1];
                replyDict.Add(postID, botCommentID);
            }
            return replyDict;
        }
        private readonly string DataDir;
        /// <summary>
        /// Main constructor, creates all data files used within this class
        /// </summary>
        public FlatBotState(string dataDir = "./Data/")
        {
            Directory.CreateDirectory(dataDir);
            DataDir = dataDir;
            if (File.Exists(dataDir + "ReplyTracker.txt"))
            { // takes the old reply checking file and updates it to the new format
                CommentDictionary = ReadReplyTrackingFile(dataDir + "ReplyTracker.txt");
                File.Delete(dataDir + "ReplyTracker.txt");
            }
            else
            { //Dictonary of replies
                if (File.Exists(dataDir + "Dictionary.json"))
                {
                    CommentDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(dataDir + "Dictionary.json")) ?? new Dictionary<string, string>();
                }
                else
                {
                    CommentDictionary = new Dictionary<string, string>();
                    File.Create(dataDir + "Dictionary.json").Dispose();
                }
            }
            if (File.Exists(dataDir + "CheckedComments.json"))
            {
                CheckedComments = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(dataDir + "CheckedComments.json")) ?? new List<string>();
            }
            else
            {
                CheckedComments = new List<string>();
                File.Create(dataDir + "CheckedComments.json").Dispose();
            }
            if (File.Exists(dataDir + "CheckedPosts.json"))
            {
                CheckedPosts = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(dataDir + "CheckedPosts.json")) ?? new List<string>();
            }
            else
            {
                CheckedPosts = new List<string>();
                File.Create(dataDir + "CheckedPosts.json").Dispose();
            }
            if (File.Exists(dataDir + "ReArchivedPosts.json"))
            {
                ReArchviedPosts = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(dataDir + "ReArchivedPosts.json")) ?? new Dictionary<string, bool>();
            }
            else
            {
                ReArchviedPosts = new Dictionary<string, bool>();
                File.Create(dataDir + "ReArchivedPosts.json").Dispose();
            }
        }
        private enum DictionaryEnum
        {
            Dictionary,
            Comments,
            Posts,
            ReArchivePosts,
            All
        }
        private void DumpDictionary(DictionaryEnum en)
        {
            switch (en)
            {
                case DictionaryEnum.All:
                    File.WriteAllText(DataDir + "Dictionary.json", JsonConvert.SerializeObject(CommentDictionary));
                    File.WriteAllText(DataDir + "CheckedComments.json", JsonConvert.SerializeObject(CheckedComments));
                    File.WriteAllText(DataDir + "CheckedPosts.json", JsonConvert.SerializeObject(CheckedPosts));
                    File.WriteAllText(DataDir + "ReArchivedPosts.json", JsonConvert.SerializeObject(ReArchviedPosts));
                    break;
                case DictionaryEnum.ReArchivePosts:
                    File.WriteAllText(DataDir + "ReArchivedPosts.json", JsonConvert.SerializeObject(ReArchviedPosts));
                    break;
                case DictionaryEnum.Comments:
                    File.WriteAllText(DataDir + "CheckedComments.json", JsonConvert.SerializeObject(CheckedComments));
                    break;
                case DictionaryEnum.Posts:
                    File.WriteAllText(DataDir + "CheckedPosts.json", JsonConvert.SerializeObject(CheckedPosts));
                    break;
                case DictionaryEnum.Dictionary:
                    File.WriteAllText(DataDir + "Dictionary.json", JsonConvert.SerializeObject(CommentDictionary));
                    break;
            }
        }
        [JsonProperty("CheckedComments")]
        List<string> CheckedComments;
        [JsonProperty("CommentDictionary")]
        Dictionary<string, string> CommentDictionary;
        [JsonProperty("CheckedPosts")]
        List<string> CheckedPosts;
        [JsonProperty("ReArchivedPosts")]
        Dictionary<string, bool> ReArchviedPosts;
        /// <inheritdoc />
        public void AddBotComment(string postID, string commentID)
        {
            CommentDictionary.Add(postID, commentID);
            DumpDictionary(DictionaryEnum.Dictionary);
        }
        /// <inheritdoc />
        public void UpdateBotComment(string postID, string commentID)
        {
            CommentDictionary[postID] = commentID;
            DumpDictionary(DictionaryEnum.Dictionary);
        }
        /// <summary>
        /// Adds a comment to the checked list
        /// </summary>
        /// <param name="commentID">Comment Id to add <see cref="RedditSharp.Things.Thing.Id"/></param>
        public void AddCheckedComment(string commentID)
        {
            CheckedComments.Add(commentID);
            DumpDictionary(DictionaryEnum.Comments);
        }
        /// <summary>
        /// COMMENT EXISTING FOR POST
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        public bool DoesCommentExist(string postID) => CommentDictionary.ContainsKey(postID);
        /// <summary>
        /// Gets the comment for post
        /// </summary>
        /// <param name="postID">Post that you replied to <seealso cref="RedditSharp.Things.Comment.LinkId"/></param>
        /// <returns>A comment ID <seealso cref="RedditSharp.Things.Thing.Id"/></returns>
        public string GetCommentForPost(string postID) => CommentDictionary[postID];
        /// <summary>
        /// Checks if the comment is checked
        /// </summary>
        /// <param name="CommentID">Comment ID of comment to check</param>
        /// <returns>If the comment exists in the checked comments dictionary</returns>
        public bool HasCommentBeenChecked(string CommentID) => CheckedComments.Contains(CommentID);

        /// <summary>
        /// Adds a post to the checked list
        /// </summary>
        /// <param name="postId">Post ID to add to checked list</param>
        public void AddCheckedPost(string postId)
        {
            CheckedPosts.Add(postId);
            DumpDictionary(DictionaryEnum.Posts);
            ReArchviedPosts.Add(postId, false);
            DumpDictionary(DictionaryEnum.ReArchivePosts);
        }
        /// <summary>
        /// Checks if a post has been checked
        /// </summary>
        /// <param name="postId">Post ID to check</param>
        /// <returns>If the post has been checked</returns>
        public bool HasPostBeenChecked(string postId) => CheckedPosts.Contains(postId);
        /// <summary>
        /// Checks if it has been archived after 24 hours
        /// </summary>
        /// <param name="postId">A post ID to add to the thing</param>
        /// <returns>A boolean stating whether it has been archived or not</returns>
        public bool Is24HourArchived(string postId) => ReArchviedPosts.ContainsKey(postId) ? ReArchviedPosts[postId] : false;
        /// <summary>
        /// Sets the post to having been archived after 24 hours
        /// </summary>
        /// <param name="postId">A <see cref="RedditSharp.Things.Post"/> ID used</param>
        /// <remarks>This assumes you've actually handled archiving it yourself</remarks>
        public void Archive24Hours(string postId)
        {
            ReArchviedPosts[postId] = true;
            DumpDictionary(DictionaryEnum.ReArchivePosts);
        }
        /// <summary>
        /// Gets all posts that have not been archived after 24 hours yet
        /// </summary>
        /// <returns>A <see cref="string[]"/> of postIDs that have not been checked yet<returns>
        public string[] GetNon24HourArchivedPosts()
        {
            return (from a in ReArchviedPosts where !a.Value select a.Key).ToArray();
        }
        public string[] GetAllCheckedComments() => CheckedComments.ToArray();

        public string[] GetAllCheckedPosts() => CheckedPosts.ToArray();

        public Dictionary<string, string> GetAllBotComments() => CommentDictionary;
        public Dictionary<string, bool> GetAllPosts24Hours() => ReArchviedPosts;

        public void DeletePostChecked(string postID)
        {
            CheckedPosts.Remove(postID);
            DumpDictionary(DictionaryEnum.Posts);
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DumpDictionary(DictionaryEnum.All);
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FlatBotState() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}