using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3
{
    public class SquareSquareControl : SquareControl
    {
        public override SquareType SquareType => SquareType.Square;

        public SquareSquareControl() : base()
        {
            SetSquarePicture(Match3.Properties.Resources.square);
        }
    }
}
