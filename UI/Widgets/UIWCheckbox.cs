using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ElementEngine
{
    public class UIWCheckbox : UIWidget, IDisposable
    {
        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //_buttonSprite?.Dispose();
                    //_buttonPressedSprite?.Dispose();
                    //_buttonHoverSprite?.Dispose();
                    //_buttonDisabledSprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public override void Load(UIFrame parent, XElement el)
        {
        }
    }
}
