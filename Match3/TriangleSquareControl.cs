using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3
{
    public class TriangleSquareControl : SquareControl
    {
        public override SquareType SquareType => SquareType.Triangle;

        public TriangleSquareControl() : base()
        {
            SetSquarePicture(Match3.Properties.Resources.triangle);
        }
    }
}
