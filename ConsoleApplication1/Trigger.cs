using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    internal class Trigger
    {
        public TaskCompletionSource<Context> Source;
        public Regex Pattern;
        public Guid GroupId;
        public int GroupIndex;
    }
}