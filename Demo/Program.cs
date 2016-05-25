using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Asimov;

namespace Asimov
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// [IN]:  Bill comes into the room
    /// [OUT]: draw sword
    /// [IN]:  You drew your sword from the sheath
    /// [OUT]: kill bill
    /// [IN]:  Bill dodged
    /// [OUT]: Try again
    /// [OUT]: kill bill
    /// [IN]:  Bill comes into the room
    /// [IN]:  you are hit
    /// [OUT]: run Forest run!
    /// [OUT]: State: Hit: True, Thirsty: False
    /// [IN]:  you are thirsty
    /// [OUT]: drink water baby
    /// [OUT]: State: Hit: True, Thirsty: True
    /// [IN]:  Bill dodged
    /// [OUT]: Try again
    /// [OUT]: kill bill
    /// [IN]:  Bill dodged
    /// [OUT]: Try again
    /// [OUT]: kill bill
    /// [IN]:  Bill is killed
    /// [OUT]: Good! Bill is killed!
    /// [OUT]: unwield sword
    /// [OUT]: rest
    /// ]]>
    /// </remarks>

    class Program
    {
        static void Main(string[] args)
        {
            Engine.Instance
                .Configure(config => config.ReadLine = ReadLine)
                .Configure(config => config.Send = config.Send = x => Console.WriteLine($"[OUT]: {x}"));

            var p = new Player();
            p.MonitorHit(); // the created task is hot
            p.MonitorThirsty(); // the created task is hot
            p.KillBill();

            Engine.Instance.Run();
        }

        private static string ReadLine()
        {
            Console.Write("[IN]:  ");
            return Console.ReadLine();
        }
    }

    class Player
    {
        public bool IsThirsty;
        public bool IsHit;

        /// <summary>
        /// Demo forked conditions
        /// </summary>
        public async void KillBill()
        {
            await "^Bill comes into the room".Wait();
            "draw sword".Send();

            await "^You drew your sword from the sheath".Wait();

            bool killed = false;
            while (!killed)
            {
                "kill bill".Send();

                if ((await new[] { "^Bill is killed", "^Bill dodged" }.Wait()).Condition == 0)
                {
                    "Good! Bill is killed!".Send();
                    "unwield sword".Send();
                    killed = true;
                }
                else
                {
                    "Try again".Send();
                }
            }

            "rest".Send();
        }

        public async void MonitorHit()
        {
            while (true)
            {
                await "^you are hit".Wait();
                IsHit = true;
                "run Forest run!".Send();
                $"State: {this}".Send();
            }
        }

        public async void MonitorThirsty()
        {
            while (true)
            {
                await "^you are thirsty".Wait();
                IsThirsty = true;
                "drink water baby".Send();
                $"State: {this}".Send();
            }
        }

        public override string ToString()
        {
            return $"Hit: {IsHit}, Thirsty: {IsThirsty}";
        }
    }
}
