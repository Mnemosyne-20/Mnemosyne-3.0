using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mnemosyne3;
using System.Collections.Generic;
using System.Linq;
namespace MnemosyneTest
{
    [TestClass]
    public class RegularExpressionsUnitTest
    {
        readonly string TestString = "\"https://www.nearlyfreespeech.net/\"" + "is supposed to be good.";
        [TestMethod]
        public void TestFindLinks()
        {
            Assert.IsTrue(RegularExpressions.FindLinks(TestString).Contains("https://www.nearlyfreespeech.net/"));
        }
        [TestMethod]
        public void TestFindLinksE()
        {
            Assert.IsTrue(RegularExpressions.FindLinksE(TestString).Contains("https://www.nearlyfreespeech.net/"));
        }
        [TestMethod]
        public void TestSameResults()
        {
            List<string> list1 = RegularExpressions.FindLinks(TestString);
            IEnumerable<string> list2 = RegularExpressions.FindLinksE(TestString);
            Assert.IsTrue(list1.TrueForAll((s) => { return list2.Contains(s); }));
        }
    }
}
