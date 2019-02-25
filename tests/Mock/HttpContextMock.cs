using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.OlapAnalysis.Mock
{
    public class HttpContextMock
    {
        public HttpContextMock() : this(new Dictionary<string, StringValues>())
        {
        }

        public HttpContextMock(IDictionary<string, StringValues> parameters)
        {
            Response = new Mock<HttpResponse>(MockBehavior.Strict);

            Request = new Mock<HttpRequest>(MockBehavior.Strict);

            Request.Setup(res => res.Query).Returns(new QueryCollection(parameters));

            Context = new Mock<HttpContext>();
            Context.SetupGet(c => c.Request).Returns(Request.Object);
            Context.SetupGet(c => c.Response).Returns(Response.Object);
            Context.SetupGet(c => c.Session).Returns(new HttpSessionTest());
        }

        public HttpContext HttpContext { get { return Context.Object; } }

        public Mock<HttpContext> Context { get; set; }
        public Mock<HttpResponse> Response { get; set; }
        public Mock<HttpRequest> Request { get; set; }
        public HttpSessionTest Session { get; set; }
        public Dictionary<object, object> Items { get; set; }

    }

    public class QueryCollection : IQueryCollection
    {
        private readonly Dictionary<string, StringValues> _stringValues = new Dictionary<string, StringValues>();
        public QueryCollection(IDictionary<string, StringValues> parameters)
        {
            _stringValues.Concat(parameters);
        }

        public StringValues this[string key]
        {
            get
            {
                StringValues res;
                TryGetValue(key, out res);
                return res;
            }
        }

        public int Count => throw new NotImplementedException();

        public ICollection<string> Keys => throw new NotImplementedException();

        public bool ContainsKey(string key)
        {
            return _stringValues.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            return _stringValues.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class HttpSessionTest : ISession
    {

        public bool IsAvailable => throw new NotImplementedException();

        public string Id => throw new NotImplementedException();

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public Task LoadAsync()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            string res;
            value = null;
            if (_strings.TryGetValue(key, out res))
            {
                value = Encoding.ASCII.GetBytes(res);
                return true;
            }
            return false;
        }

        public void Set(string key, byte[] value)
        {
            if (_strings.ContainsKey(key))
                return;

            string sValue = Encoding.ASCII.GetString(value);
            _strings.Add(key, sValue);
        }

        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public void Remove(string key)
        {
            if (_ints.ContainsKey(key))
                _ints.Remove(key);

            if (_strings.ContainsKey(key))
                _strings.Remove(key);
        }

        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}
