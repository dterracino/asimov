using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class Engine
    {
        static Engine instance = new Engine();

        public static Engine Instance
        {
            get
            {
                return instance;
            }
        }

        Func<string> readline = null;
        Action<string> send = null;
        private object sync = new object();

        public void Configure(Func<string> readline, Action<string> send)
        {
            lock (sync)
            {
                this.readline = readline;
                this.send = send;
            }
        }

        List<Trigger> TriggerBag = new List<Trigger>();
        Dictionary<Guid, List<Trigger>> TriggerGroup = new Dictionary<Guid, List<Trigger>>();

        public void Run()
        {
            string line;
            while (null != (line = readline()))
            {
                Trigger triggered = MatchWith(line);
                if (triggered != null)
                {
                    triggered.Source.SetResult(new Context { Condition = triggered.GroupIndex });
                }
            }
        }

        public async Task<Context> Pattern(params string[] vs)
        {
            Guid groupId = Guid.NewGuid();
            List<Trigger> triggers = new List<Trigger>();
            List<Task<Context>> tasks = new List<Task<Context>>();
            for (int i = 0; i < vs.Length; i++)
            {
                string v = vs[i];
                var src = new TaskCompletionSource<Context>();
                triggers.Add(new Trigger { Source = src, Pattern = new Regex(v), GroupId = groupId, GroupIndex = i });
                tasks.Add(src.Task);
            }

            TriggerBag.AddRange(triggers);
            TriggerGroup[groupId] = triggers;

            var x = await await Task.WhenAny(tasks);

            foreach (var trig in TriggerGroup[groupId])
            {
                TriggerBag.Remove(trig);
                trig.Source.TrySetCanceled();
                TriggerGroup.Remove(groupId);
            }

            return x;
        }

        public void Send(string line)
        {
            send(line);
        }

        private Trigger MatchWith(string line)
        {
            foreach (var item in TriggerBag)
            {
                var res = item.Pattern.Match(line);
                if (res.Success) return item;
            }

            return null;
        }
    }

    public static class EngineExtentions
    {
        public static Task<Context> Wait(this string s)
        {
            return Engine.Instance.Pattern(s);
        }

        public static Task<Context> Wait(this IEnumerable<string> vs)
        {
            return Engine.Instance.Pattern(vs.ToArray());
        }

        public static void Send(this string line)
        {
            Engine.Instance.Send(line);
        }
    }
}
