using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Asimov
{
    internal class Trigger
    {
        public TaskCompletionSource<Context> Source;
        public Regex Pattern;
        public Guid GroupId;
        public int GroupIndex;
    }
}