using System;

namespace ConsoleApplication1
{
    public class Context
    {
        internal int Condition;
        private string line;

        public Context(string line)
        {
            this.line = line;
        }

        public void Send(string line)
        {
            Console.WriteLine("<< " + line);
        }
    }
}