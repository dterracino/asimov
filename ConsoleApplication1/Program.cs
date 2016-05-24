using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// >> Bill comes into the room
    /// << draw sword
    /// >> you are hit                      [Interleaved with other triggers]
    /// << run Forest run!
    /// << State: Hit: True, Thirsty: False
    /// >> You drew your sword from the sheath
    /// << kill bill
    /// >> Bill dodged
    /// << Try again
    /// << kill bill
    /// >> you are thirsty                  [Interleaved with other triggers]
    /// << drink water baby
    /// << State: Hit: True, Thirsty: True
    /// >> Bill dodged
    /// << Try again
    /// << kill bill
    /// >> Bill comes into the room         [Matched triggers are removed and won't match again]
    /// >> Bill is killed
    /// << Good! Bill is killed!
    /// << unwield sword
    /// << rest
    /// >>
    /// ]]>
    /// </remarks>

    class Program
    {
        static void Main(string[] args)
        {
            var engine = new Engine(ReadLine);

            var p = new Player(engine.CreateTrigger, engine.CreateTriggerN);
            p.MonitorHit(); // the created task is hot
            p.MonitorThirsty(); // the created task is hot
            p.KillBill();

            engine.Run();
        }

        private static string ReadLine()
        {
            Console.Write(">> ");
            return Console.ReadLine();
        }
    }

    class Player
    {
        Func<string, Task<Context>> createTrigger;
        Func<string[], Task<Context>> createTriggerN;
        public bool IsThirsty;
        public bool IsHit;

        public Player(
            Func<string, Task<Context>> createTrigger,
            Func<string[], Task<Context>> createTriggerN)
        {
            this.createTrigger = createTrigger;
            this.createTriggerN = createTriggerN;
        }

        /// <summary>
        /// Demo fork
        /// </summary>
        public async void KillBill()
        {
            var ctx = await createTrigger("^Bill comes into the room");
            ctx.Send("draw sword");

            ctx = await createTrigger("^You drew your sword from the sheath");

            bool killed = false;
            while (!killed)
            {
                ctx.Send("kill bill");

                ctx = await createTriggerN(new[] { "^Bill is killed", "^Bill dodged" });

                if (ctx.Condition == 0)
                {
                    ctx.Send("Good! Bill is killed!");
                    ctx.Send("unwield sword");
                    killed = true;
                }
                else
                {
                    ctx.Send("Try again");
                }
            }

            ctx.Send("rest");
        }

        public async void MonitorHit()
        {
            while (true)
            {
                var ctx = await createTrigger("^you are hit");
                IsHit = true;
                ctx.Send("run Forest run!");
                ctx.Send($"State: {this}");
            }
        }

        public async void MonitorThirsty()
        {
            while (true)
            {
                var ctx = await createTrigger("^you are thirsty");
                IsThirsty = true;
                ctx.Send("drink water baby");
                ctx.Send($"State: {this}");
            }
        }

        public override string ToString()
        {
            return $"Hit: {IsHit}, Thirsty: {IsThirsty}";
        }
    }
}
