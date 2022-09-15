using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3
{
    public class Constants
    {
        private static Constants _constants;

        private Constants()
        {

        }

        public int SquareSideSize => 64;
        public int PlayingFieldHeight => 8;
        public int PlayingFieldWidth => 8;

        public static Constants GetInstance()
        {
            if (_constants == null)
                _constants = new Constants();

            return _constants;
        }
    }
}
