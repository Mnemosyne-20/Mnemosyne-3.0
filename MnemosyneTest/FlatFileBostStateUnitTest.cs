using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mnemosyne3.BotState;
using System;
using System.Linq;
namespace MnemosyneTest
{
    [TestClass]
    public class FlatFileBostStateUnitTest
    {
        [TestInitialize]
        public void InitializeTestVars()
        {
            foreach(var directory in System.IO.Directory.EnumerateDirectories(".\\Data"))
            { 
                foreach (var item in System.IO.Directory.EnumerateFiles(directory).Where((a) => a.EndsWith("json")))
                {
                    System.IO.File.Delete(item);
                }
            }
        }
        [TestCategory("FlatFileBotState")]
        [TestMethod]
        public void TestAddBotCommentFlatFile()
        {
            FlatBotState flatBotState = new FlatBotState("./Data/1\\");
            flatBotState.AddBotComment("post", "postcomment");
            Assert.IsTrue(flatBotState.GetCommentForPost("post") == "postcomment");
        }
        [TestCategory("FlatFileBotState")]
        [TestMethod]
        public void TestCheckCommentFlatFile()
        {
            FlatBotState flatBotState = new FlatBotState("./Data/2\\");
            flatBotState.AddCheckedComment("postcomment");
            Assert.IsTrue(flatBotState.HasCommentBeenChecked("postcomment"));
        }
        [TestCategory("FlatFileBotState")]
        [TestMethod]
        public void TestCheckPostFlatFile()
        {
            FlatBotState flatBotState = new FlatBotState("./Data/3\\");
            flatBotState.AddCheckedPost("post");
            Assert.IsTrue(flatBotState.HasPostBeenChecked("post"));
        }
        [TestCategory("FlatFileBotState")]
        [TestMethod]
        public void TestUpdateCommentFlatFile()
        {
            FlatBotState flatBotState = new FlatBotState("./Data/4\\");
            flatBotState.AddBotComment("post", "postcomment");
            flatBotState.UpdateBotComment("post", "postcomment2");
            Assert.IsTrue(flatBotState.GetCommentForPost("post") == "postcomment2");
        }
        [TestCategory("FlatFileBotState")]
        [TestMethod]
        public void Test24HourArchiveFlatFile()
        {
            FlatBotState flatBotState = new FlatBotState("./Data/5\\");
            flatBotState.AddCheckedPost("post");
            Assert.IsFalse(flatBotState.Is24HourArchived("post"));
            Assert.IsFalse(flatBotState.GetNon24HourArchivedPosts().Length == 0);
            flatBotState.Archive24Hours("post");
            Assert.IsTrue(flatBotState.Is24HourArchived("post"));
            Assert.IsTrue(flatBotState.GetNon24HourArchivedPosts().Length == 0);
        }
    }
}