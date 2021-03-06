using System;

namespace Mnemosyne3.BotState
{
    public interface IBotState : IDisposable
    {
        /// <summary>
        /// Adds a post to the checked list
        /// </summary>
        /// <param name="postId">Post ID to add to checked list</param>
        void AddCheckedPost(string postId);
        /// <summary>
        /// Checks if a post has been checked
        /// </summary>
        /// <param name="postId">Post ID to check</param>
        /// <returns>If the post has been checked</returns>
        bool HasPostBeenChecked(string postId);
        /// <summary>
        /// Checks if a comment exists if given a postID
        /// </summary>
        /// <param name="postId">Post to check</param>
        /// <returns>If comment exists for post</returns>
        bool DoesCommentExist(string postId);
        /// <summary>
        /// Gets the comment ID for a post
        /// </summary>
        /// <param name="postId">POSTID for comment</param>
        /// <returns>CommentID</returns>
        string GetCommentForPost(string postId);
        /// <summary>
        /// Checks if a comment has been checked
        /// </summary>
        /// <param name="commentId">Checks if comment has been added to checked list</param>
        /// <returns>If the comment was checked</returns>
        bool HasCommentBeenChecked(string commentId);
        /// <summary>
        /// Adds a comment for post ID internally
        /// </summary>
        /// <param name="postID">Post that you commented on</param>
        /// <param name="commentID">Comment ID for the comment</param>
        void AddBotComment(string postID, string commentID);
        /// <summary>
        /// Updates a bot comment for post ID, adding this as I have run into the 10k character limit...
        /// </summary>
        /// <param name="postID"><see cref="AddBotComment(string, string)"/></param>
        /// <param name="commentID"><see cref="AddBotComment(string, string)"/></param>
        void UpdateBotComment(string postID, string commentID);
        /// <summary>
        /// Adds a comment to checked list
        /// </summary>
        /// <param name="commentID">CommentID</param>
        void AddCheckedComment(string commentID);
        /// <summary>
        /// Checks if it has been archived after 24 hours
        /// </summary>
        /// <param name="postID">A post ID to add to the thing</param>
        /// <returns>A boolean stating whether it has been archived or not</returns>
        bool Is24HourArchived(string postID);
        void Archive24Hours(string postID);
        /// <summary>
        /// Gets all posts that haven't been 24 hour archived
        /// </summary>
        /// <returns>All non-24 hour archived posts</returns>
        string[] GetNon24HourArchivedPosts();
        /// <summary>
        /// Delets a post that has been checked before
        /// This endpoint assumes it already exists, calling it on something that doesn't kills this
        /// </summary>
        /// <param name="postID">A previously checked post</param>
        void DeletePostChecked(string postID);
    }
}