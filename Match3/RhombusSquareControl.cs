using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3
{
    public class RhombusSquareControl : SquareControl
    {
        public override SquareType SquareType => SquareType.Rhombus;

        public RhombusSquareControl() : base()
        {
            SetSquarePicture(Match3.Properties.Resources.rhombus);
        }
    }
}
