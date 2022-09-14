using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Match3
{
    public partial class SquareControl : UserControl
    {
        private Panel _selectedSquarePanel;
        private LineBonusOrientation _lineBonusOrientation;

        private bool _lineBonus;
        private bool _bombBonus;

        public virtual SquareType SquareType
        {
            get
            {
                return SquareType.None;
            }
        }

        public SquarePosition Position
        {
            get
            {
                return new SquarePosition
                {
                    SquareControl = this,
                    Row = this.Location.Y / 64,
                    Column = this.Location.X / 64
                };
            }
        }

        public bool LineBonus
        {
            get
            {
                return _lineBonus;
            }
        }

        public LineBonusOrientation LineBonusOrientation
        {
            get
            {
                return _lineBonusOrientation;
            }
        }

        public bool BombBonus
        {
            get
            {
                return _bombBonus;
            }

            set
            {
                if(value && !_lineBonus)
                    pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);

                _bombBonus = value;
            }
        }

        public SquareControl()
        {
            InitializeComponent();

            _selectedSquarePanel = new Panel
            {
                Parent = pictureBox1,
                BackColor = Color.FromArgb(100, Color.Red),
                Dock = DockStyle.Fill,
                Visible = false
            };
        }

        public EventHandler SetSelectedSquarePanels;

        public bool IsSelectedSquare
        {
            get
            {
                return _selectedSquarePanel.Visible;
            }

            set
            {
                _selectedSquarePanel.Visible = value;
            }
        }

        public void SetSquarePicture(Bitmap image)
        {
            this.pictureBox1.BackgroundImage = image;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (SetSelectedSquarePanels != null)
                SetSelectedSquarePanels(this, e);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_lineBonus)
            {
                Pen pen = new Pen(Color.Black, 3);
                e.Graphics.DrawEllipse(pen, _lineBonusOrientation == LineBonusOrientation.Horizontal ? 2 : 32, _lineBonusOrientation == LineBonusOrientation.Horizontal ? 32 : 2, _lineBonusOrientation == LineBonusOrientation.Horizontal ? 60 : 10, _lineBonusOrientation == LineBonusOrientation.Horizontal ? 10 : 60);
            }

            if (_bombBonus)
            {
                Pen pen = new Pen(Color.Black);
                Brush brush = pen.Brush;
                e.Graphics.FillEllipse(brush, 27, 27, 10, 10);
            }
        }

        public void SetLineBonusOrientation(LineBonusOrientation lineBonusOrientation)
        {
            _lineBonusOrientation = lineBonusOrientation;

            if (_lineBonusOrientation == LineBonusOrientation.Horizontal || _lineBonusOrientation == LineBonusOrientation.Vertiсal)
                _lineBonus = true;

            if(!_bombBonus)
                pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);

            Refresh();
        }
    }
}
