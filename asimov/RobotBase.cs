using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asimov
{
    class RobotBase : IRobot
    {
        readonly Guid id = Guid.NewGuid();
        Engine engine;
        public RobotBase(Engine engine)
        {
            this.engine = engine;
        }

        public Task RunAsync()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        protected async Task<Context> For(params string[] patterns)
        {
            return await engine.Pattern(patterns);
        }
    }
}
