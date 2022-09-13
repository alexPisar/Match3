using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3
{
    public class CircleSquareControl : SquareControl
    {
        public override SquareType SquareType => SquareType.Circle;

        public CircleSquareControl() : base()
        {
            SetSquarePicture(Match3.Properties.Resources.Circle);
        }
    }
}
