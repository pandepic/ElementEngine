using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public interface IEngineService
    {
        public void HandleMessage<T>(T message) where T : struct, IServiceMessage;
    }
}
