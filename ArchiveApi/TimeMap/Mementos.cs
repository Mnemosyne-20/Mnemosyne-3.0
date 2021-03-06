using ArchiveApi.LinkParsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace ArchiveApi
{
    internal static class Extension
    {
        internal static string GetAttrValues(this WebLink link, string val) => link.Parameters[val];
        internal static string GetAttrRelValues(this WebLink link) => link.Parameters["rel"];
    }
    public class Memento : IEquatable<Uri>, IEquatable<string>
    {
        public bool IsFirst => webLink.GetAttrRelValues().Contains("first");
        public bool IsLast => webLink.GetAttrRelValues().Contains("last");
        public DateTime TimeArchived => DateTime.Parse(string.Join(" ", webLink.GetAttrValues("datetime")));
        public bool IsActuallyMemento => webLink.GetAttrRelValues().Contains("memento");
        public Uri Url => webLink.Link;
        WebLink webLink;
        public Memento(WebLink memento) => webLink = memento;
        #region Equality operators
        public override bool Equals(object other) => (other is WebLink) && ((other as WebLink) == webLink);
        public static bool operator ==(Memento mem1, Memento mem2) => mem1.Equals(mem2);
        public static bool operator !=(Memento mem1, Memento mem2) => !mem1.Equals(mem2);
        public static bool operator ==(Memento mem1, string mem2) => mem1.Equals(mem2);
        public static bool operator !=(Memento mem1, string mem2) => !mem1.Equals(mem2);
        public static bool operator ==(Memento mem1, Uri mem2) => mem1.Equals(mem2);
        public static bool operator !=(Memento mem1, Uri mem2) => !mem1.Equals(mem2);
        public bool Equals(Uri other) => Url == other;
        public bool Equals(string other) => Url.ToString() == other;
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
    public class Mementos : IEnumerable
    {
        public string TimeGate { get; private set; } = null;
        public TimeMap TimeMap { get; private set; } = null;
        public string Original { get; private set; } = null;
        public Memento FirstMemento => _mementos[0];
        public Memento LastMemento => _mementos[_mementos.Length - 1];
        Memento[] _mementos;
        public Mementos(IEnumerable<Memento> mementoList)
        {
            _mementos = mementoList.ToArray();
        }
        public Mementos(IEnumerable<WebLink> mementoList)
        {
            _mementos = new Memento[((mementoList.Where(a => a.GetAttrRelValues().Contains("memento"))).Count())];
            for (int i = 0, i2 = 0; i < mementoList.Count(); i++)
            {
                var link = mementoList.ElementAt(i);
                var linkAttributes = link.GetAttrRelValues();
                if (linkAttributes.Contains("timegate"))
                    TimeGate = link.Link.ToString();
                else if (linkAttributes.Contains("timemap"))
                    TimeMap = new TimeMap(link.Link);
                else if (linkAttributes.Contains("original"))
                    Original = link.Link.ToString();
                else if (linkAttributes.Contains("memento"))
                {
                    _mementos[i2] = new Memento(link);
                    i2++;
                }
            }
        }
        public IEnumerator GetEnumerator() => new MementoEnumerator(_mementos);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
    public class MementoEnumerator : IEnumerator
    {
        public Memento[] mementos;
        int position = -1;
        public MementoEnumerator(Memento[] mementos) => this.mementos = mementos;
        public Memento Current
        {
            get
            {
                try
                {
                    return mementos[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            position++;
            return (position < mementos.Length);
        }

        public void Reset() => position = -1;
    }
}
