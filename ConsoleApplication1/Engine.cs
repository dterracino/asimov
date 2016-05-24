using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Engine
    {
        Func<string> readline = null;

        public Engine(Func<string> readline)
        {
            this.readline = readline;
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
                    triggered.Source.SetResult(new Context(line) { Condition = triggered.GroupIndex });
                }
            }
        }

        public async Task<Context> CreateTrigger(string v)
        {
            return await CreateTriggerN(v);
        }

        public async Task<Context> CreateTriggerN(params string[] vs)
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
            }

            return x;
        }

        private Trigger MatchWith(string line)
        {
            foreach (var item in TriggerBag)
            {
                var res = item.Pattern.Match(line);
                if (res.Success)
                {
                    TriggerBag.Remove(item);

                    return item;
                }
            }

            return null;
        }
    }
}
