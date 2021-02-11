using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.EC
{
    public interface IComponent
    {
        public void Update(GameTimer gameTimer);
    }
}
