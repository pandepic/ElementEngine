using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public interface IEngineService
    {
        public BaseGame Parent { get; set; }
        public void HandleMessage(IServiceMessage message);
    }
}
