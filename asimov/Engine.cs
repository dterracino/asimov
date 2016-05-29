using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Asimov
{
    public class Engine
    {
        public class Configuration
        {
            public Func<string> ReadLine;
            public Action<string> Send;
        }

        static Engine instance = new Engine();

        public static Engine Instance
        {
            get
            {
                return instance;
            }
        }

        private object sync = new object();
        private Configuration config = new Configuration();

        public Engine Configure(Action<Configuration> configure)
        {
            lock (sync)
            {
                configure(this.config);
                return this;
            }
        }

        List<Trigger> TriggerBag = new List<Trigger>();
        Dictionary<Guid, List<Trigger>> TriggerGroup = new Dictionary<Guid, List<Trigger>>();

        public void Run()
        {
            string line;
            while (null != (line = config.ReadLine()))
            {
                Trigger triggered = MatchWith(line);
                if (triggered != null)
                {
                    triggered.Source.SetResult(new Context { Condition = triggered.ConditionIndex });
                }
            }
        }

        public Task<Context> Pattern(params string[] vs)
        {
            return Pattern(Guid.NewGuid(), vs);
        }

        public async Task<Context> Pattern(Guid groupId, params string[] vs)
        {
            Guid condId = Guid.NewGuid();
            List<Trigger> triggers = new List<Trigger>();
            List<Task<Context>> tasks = new List<Task<Context>>();
            for (int i = 0; i < vs.Length; i++)
            {
                string v = vs[i];
                var src = new TaskCompletionSource<Context>();
                triggers.Add(new Trigger { Source = src, Pattern = new Regex(v), ConditionId = condId, ConditionIndex = i, GroupId = groupId });
                tasks.Add(src.Task);
            }

            TriggerBag.AddRange(triggers);
            TriggerGroup[condId] = triggers;

            var x = await await Task.WhenAny(tasks);

            foreach (var trig in TriggerGroup[condId])
            {
                TriggerBag.Remove(trig);
                trig.Source.TrySetCanceled();
                TriggerGroup.Remove(condId);
            }

            return x;
        }

        public void Send(string line)
        {
            config.Send(line);
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
