using ArchiveApi.Interfaces;
using ArchiveApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mnemosyne3;
using RedditSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
namespace MnemosyneTest
{
    public class MockService : IArchiveService
    {
        public Uri TimeMapEndpoint => new Uri(BaseUri, "/timemap");

        public Uri BaseUri => new Uri("http://what.com");

        public Uri SubmitEndpoint => new Uri(BaseUri, "/test");

        public string Save(string Url)
        {
            return SubmitEndpoint.ToString();
        }

        public Uri Save(Uri Url)
        {
            return SubmitEndpoint;
        }

        public Task<string> SaveAsync(string Url)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> SaveAsync(Uri Url)
        {
            throw new NotImplementedException();
        }

        public bool Verify(string Url)
        {
            return Url == SubmitEndpoint.ToString();
        }

        public bool Verify(Uri Url)
        {
            return Url == SubmitEndpoint;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MockService() {
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
    [TestClass]
    public class ArchiveLinksUnitTest
    {
        [TestInitialize]
        public void Initialize()
        {
            red = new Reddit();
            System.IO.Directory.CreateDirectory(".\\Data\\1");
            foreach (var directory in System.IO.Directory.EnumerateDirectories(".\\Data"))
            {
                foreach (var item in System.IO.Directory.EnumerateFiles(directory).Where((a) => a.EndsWith("Testing.sqlite")))
                {
                    System.IO.File.Delete(item);
                }
            }
        }
        Reddit red;
        [TestMethod]
        public void ArchiveLinksTest()
        {
            IArchiveService service = new MockService();
            new Mnemosyne3.UserData.RedditUserProfileSqlite("1\\Testing.sqlite");
            List<string> test = new List<string>()
            {
                "https://www.example.com",
                "http://example.com?test=test",
                "http://example.com?test=test&test"
            };
            ArchiveLinks.ArchivePostLinks(test, new Regex[] { new Regex("") }, red.GetUserAsync("chugga_fan").Result, service);
            Mnemosyne3.UserData.RedditUserProfileSqlite redditUserProfileSqlite = new Mnemosyne3.UserData.RedditUserProfileSqlite(red.GetUserAsync("chugga_fan").Result);
            Assert.IsTrue(redditUserProfileSqlite.Unarchived == 3);
        }
    }
}
