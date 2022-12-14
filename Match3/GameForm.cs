using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace Match3
{
    public partial class GameForm : Form
    {
        private SquarePosition _currentPosition;
        private int _numberOfPoints;
        private SquareControl[,] _squares = new SquareControl[Constants.GetInstance().PlayingFieldHeight, Constants.GetInstance().PlayingFieldWidth];
        private System.Windows.Forms.Timer GameTime;
        private int _remainingSeconds;
        private List<SquarePosition> _newLineBonuses;
        private List<SquarePosition> _newBombBonuses;

        public GameForm()
        {
            InitializeComponent();
            DialogResult = System.Windows.Forms.DialogResult.None;
            _numberOfPoints = 0;

            _remainingSeconds = 60;
            GameTime = new System.Windows.Forms.Timer();
            GameTime.Tick += new EventHandler(TimeIsUp);
            GameTime.Interval = 1000;
        }

        private void Form_load(object sender, EventArgs e)
        {
            Random rnd = new Random();

            for (int i = 0; i < Constants.GetInstance().PlayingFieldHeight; i++)
                for (int j = 0; j < Constants.GetInstance().PlayingFieldWidth; j++)
                {
                    SquareControl square;

                    //В начальном заполнении не допускается, чтобы три квадрата оказались в ряду
                    while (true)
                    {
                        var value = rnd.Next() % 5;

                        square = GetSquareControlByNumber(value);

                        if (i < 2 && j < 2)
                            break;

                        if(i > 1)
                        {
                            var s1 = _squares[i - 1, j];
                            var s2 = _squares[i - 2, j];

                            if (s1.SquareType == square.SquareType && s2.SquareType == square.SquareType)
                                continue;
                            else if (j < 2)
                                break;
                        }

                        if(j > 1)
                        {
                            var s1 = _squares[i, j - 1];
                            var s2 = _squares[i, j - 2];

                            if (s1.SquareType == square.SquareType && s2.SquareType == square.SquareType)
                                continue;
                            else
                                break;
                        }
                    }

                    square.Dock = DockStyle.None;
                    square.Size = new Size(Constants.GetInstance().SquareSideSize, Constants.GetInstance().SquareSideSize);

                    square.SetSelectedSquarePanels += SetSelectedSquarePanel;

                    _squares[i, j] = square;
                    panel1.Controls.Add(square);

                    square.Location = new Point(j * Constants.GetInstance().SquareSideSize, i * Constants.GetInstance().SquareSideSize);
                }

            //Квадраты заполнили - и можно игру начать
            GameTime.Start();
        }

        private void TimeIsUp(object sender, EventArgs e)
        {
            minutesLabel.Text = "00";

            if(_remainingSeconds > 0)
                _remainingSeconds--;

            if (_remainingSeconds == 0)
            {
                SecundesLabel.Text = "00";

                if (!panel1.Enabled)
                    return;

                GameTime.Stop();

                var gameOverForm = new GameOverForm(_numberOfPoints);

                if (gameOverForm.ShowDialog() == DialogResult.OK)
                    DialogResult = DialogResult.OK;

                this.Owner.Visible = true;
                this.Close();
            }
            else
                SecundesLabel.Text = _remainingSeconds < 10 ? "0" + Convert.ToString(_remainingSeconds) : Convert.ToString(_remainingSeconds);
        }

        private async void SetSelectedSquarePanel(object s, EventArgs args)
        {
            for (int indx = 0; indx < Constants.GetInstance().PlayingFieldHeight; indx++)
                for (int jndx = 0; jndx < Constants.GetInstance().PlayingFieldWidth; jndx++)
                {
                    var sq = _squares[indx, jndx];
                    sq.IsSelectedSquare = false;
                }

            var lastPosition = _currentPosition;

            _currentPosition = ((SquareControl)s).Position;

            if (lastPosition == null)
            {
                ((SquareControl)s).IsSelectedSquare = true;
                return;
            }

            if ((Math.Abs(lastPosition.Row - _currentPosition.Row) == 1 && lastPosition.Column == _currentPosition.Column) ||
            (Math.Abs(lastPosition.Column - _currentPosition.Column) == 1 && lastPosition.Row == _currentPosition.Row))
            {
                ChangePlacesTwoSquarePositions(lastPosition, _currentPosition);

                _newLineBonuses = new List<SquarePosition>();
                _newBombBonuses = new List<SquarePosition>();

                var deletedPositions = GetDeletedSquaresWithPositions(lastPosition);
                deletedPositions.AddRange(GetDeletedSquaresWithPositions(_currentPosition));

                GetAllDeletedWithBonusesSquares(deletedPositions);

                deletedPositions = deletedPositions.Where(p => (!_newBombBonuses.Exists(b => b.Row == p.Row && b.Column == p.Column)) && (!_newLineBonuses.Exists(l => l.Row == p.Row && l.Column == p.Column))).ToList();

                _numberOfPoints = _numberOfPoints + deletedPositions.Count;

                if (deletedPositions.Count == 0)
                {
                    ChangePlacesTwoSquarePositions(lastPosition, _currentPosition);
                }
                else
                {
                    panel1.Enabled = false;
                    SetEnableStateForSquares(false);

                    var columnGroupsDeletedPositions = from d in deletedPositions
                                                       group d by d.Column;

                    DeleteSquareControlsOnPositions(deletedPositions);

                    List<Task> tasks = new List<Task>();
                    foreach (var group in columnGroupsDeletedPositions)
                    {
                        var task = MoveDownElementsInColumn(group);
                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);

                    await DoWhileExistsThreeSquaresLines();

                    panel1.Enabled = true;
                    SetEnableStateForSquares(true);
                }
            }

            if (_currentPosition != null)
            {
                _squares[_currentPosition.Row, _currentPosition.Column].IsSelectedSquare = true;
                _currentPosition.SquareControl = _squares[_currentPosition.Row, _currentPosition.Column];
            }

            label2.Text = Convert.ToString(_numberOfPoints);
        }

        private void SetEnableStateForSquares(bool enableValue)
        {
            for(int i = 0; i < Constants.GetInstance().PlayingFieldHeight; i++)
            {
                for(int j = 0; j < Constants.GetInstance().PlayingFieldWidth; j++)
                {
                    _squares[i, j].Enabled = enableValue;
                }
            }
        }

        private void ChangePlacesTwoSquarePositions(SquarePosition position1, SquarePosition position2)
        {
            _squares[position2.Row, position2.Column].Refresh();
            _squares[position1.Row, position1.Column].Refresh();

            if (Math.Abs(position1.Row - position2.Row) == 1 && position1.Column == position2.Column)
            {
                for (int i = 0; i < Constants.GetInstance().SquareSideSize / 8; i++)
                {
                    if (position1.Row > position2.Row)
                    {
                        position1.SquareControl.Location = new Point(position1.SquareControl.Location.X, position1.SquareControl.Location.Y - 8);
                        position2.SquareControl.Location = new Point(position2.SquareControl.Location.X, position2.SquareControl.Location.Y + 8);
                        Thread.Sleep(20);
                    }
                    else
                    {
                        position1.SquareControl.Location = new Point(position1.SquareControl.Location.X, position1.SquareControl.Location.Y + 8);
                        position2.SquareControl.Location = new Point(position2.SquareControl.Location.X, position2.SquareControl.Location.Y - 8);
                        Thread.Sleep(20);
                    }
                }
            }
            else if (Math.Abs(position1.Column - position2.Column) == 1 && position1.Row == position2.Row)
            {
                for (int i = 0; i < Constants.GetInstance().SquareSideSize / 8; i++)
                {
                    if (position1.Column > position2.Column)
                    {
                        position1.SquareControl.Location = new Point(position1.SquareControl.Location.X - 8, position1.SquareControl.Location.Y);
                        position2.SquareControl.Location = new Point(position2.SquareControl.Location.X + 8, position2.SquareControl.Location.Y);
                        Thread.Sleep(20);
                    }
                    else
                    {
                        position1.SquareControl.Location = new Point(position1.SquareControl.Location.X + 8, position1.SquareControl.Location.Y);
                        position2.SquareControl.Location = new Point(position2.SquareControl.Location.X - 8, position2.SquareControl.Location.Y);
                        Thread.Sleep(20);
                    }
                }
            }

            var sqControl = position1.SquareControl;
            position1.SquareControl = position2.SquareControl;
            position2.SquareControl = sqControl;

            _squares[position2.Row, position2.Column] = position2.SquareControl;
            _squares[position1.Row, position1.Column] = position1.SquareControl;
        }

        private void DeleteSquareControlsOnPositions(List<SquarePosition> positions)
        {
            if (positions.Exists(p => p.Row == _currentPosition?.Row && p.Column == _currentPosition?.Column))
                _currentPosition = null;

            foreach (var pos in positions)
            {
                panel1.Controls.Remove(pos.SquareControl);
                pos.SquareControl.Visible = false;
                _squares[pos.Row, pos.Column] = null;
            }
        }

        private SquareControl GetSquareControlByNumber(int value)
        {
            SquareControl square;

            switch (value)
            {
                case 0:
                    square = new CircleSquareControl();
                    break;
                case 1:
                    square = new PentagonSquareControl();
                    break;
                case 2:
                    square = new RhombusSquareControl();
                    break;
                case 3:
                    square = new SquareSquareControl();
                    break;
                case 4:
                    square = new TriangleSquareControl();
                    break;
                default:
                    square = new SquareControl();
                    break;
            }

            return square;
        }

        private List<SquarePosition> GetDeletedSquaresWithPositions(SquarePosition position)
        {
            var squares = new List<SquarePosition>();

            bool passToLeft = false;
            bool passToRight = false;
            bool passUp = false;
            bool passDown = false;

            Func<int, int, bool> EqualityInColumn = (int a, int b) => 
            {
                var s1 = _squares[position.Row, a];
                var s2 = _squares[position.Row, b];

                return position.SquareControl.SquareType == s1.SquareType && position.SquareControl.SquareType == s2.SquareType;
            };

            Func<int, int, bool> EqualityInRow = (int a, int b) =>
            {
                var s1 = _squares[a, position.Column];
                var s2 = _squares[b, position.Column];

                return position.SquareControl.SquareType == s1.SquareType && position.SquareControl.SquareType == s2.SquareType;
            };

            if (position.Column > 0 && position.Column < Constants.GetInstance().PlayingFieldWidth - 1)
            {
                if(EqualityInColumn(position.Column - 1, position.Column + 1))
                {
                    passToLeft = true;
                    passToRight = true;
                }
            }

            if (position.Column > 1)
            {
                if (EqualityInColumn(position.Column - 1, position.Column - 2))
                {
                    passToLeft = true;
                }
            }

            if (position.Column < Constants.GetInstance().PlayingFieldWidth - 2)
            {
                if (EqualityInColumn(position.Column + 1, position.Column + 2))
                {
                    passToRight = true;
                }
            }

            if (position.Row > 0 && position.Row < Constants.GetInstance().PlayingFieldHeight - 1)
            {
                if (EqualityInRow(position.Row - 1, position.Row + 1))
                {
                    passUp = true;
                    passDown = true;
                }
            }

            if (position.Row > 1)
            {
                if (EqualityInRow(position.Row - 1, position.Row - 2))
                {
                    passUp = true;
                }
            }

            if (position.Row < Constants.GetInstance().PlayingFieldHeight - 2)
            {
                if (EqualityInRow(position.Row + 1, position.Row + 2))
                {
                    passDown = true;
                }
            }

            if (passToLeft)
            {
                var c = position.Column - 1;
                var s = _squares[position.Row, c];

                while (s.SquareType == position.SquareControl.SquareType)
                {
                    squares.Add(s.Position);

                    c--;

                    if (c < 0)
                        break;

                    s = _squares[position.Row, c];
                }
            }

            if (passToRight)
            {
                var c = position.Column + 1;
                var s = _squares[position.Row, c];

                while (s.SquareType == position.SquareControl.SquareType)
                {
                    squares.Add(s.Position);

                    c++;

                    if (c > Constants.GetInstance().PlayingFieldWidth - 1)
                        break;

                    s = _squares[position.Row, c];
                }
            }

            if(squares.Count == 3 && !squares.Exists(s => s?.SquareControl?.LineBonusOrientation == LineBonusOrientation.Horizontal) && !position.SquareControl.LineBonus)
            {
                position.SquareControl.SetLineBonusOrientation(LineBonusOrientation.Horizontal);
                _newLineBonuses.Add(position);
                _numberOfPoints++;
            }

            if (squares.Count >= 4 && !_newBombBonuses.Exists(n => squares.Exists(sq => sq.Row == n.Row && sq.Column == n.Column)) && !position.SquareControl.BombBonus)
            {
                position.SquareControl.BombBonus = true;
                _newBombBonuses.Add(position);
                _numberOfPoints++;
            }

            int horizontalPositionCount = squares.Count;
            int verticalPositionCount = 0;
            bool existsInVertikalPositionLineBonus = false;
            bool existsInVertikalPositionBombBonus = false;

            if (passUp)
            {
                var c = position.Row - 1;
                var s = _squares[c, position.Column];

                while (s.SquareType == position.SquareControl.SquareType)
                {
                    squares.Add(s.Position);

                    verticalPositionCount++;
                    existsInVertikalPositionLineBonus = existsInVertikalPositionLineBonus || s.LineBonusOrientation == LineBonusOrientation.Vertiсal;
                    existsInVertikalPositionBombBonus = existsInVertikalPositionBombBonus || _newBombBonuses.Exists(n => n.Row == s.Position.Row && n.Column == s.Position.Column);
                    c--;

                    if (c < 0)
                        break;

                    s = _squares[c, position.Column];
                }
            }

            if (passDown)
            {
                var c = position.Row + 1;
                var s = _squares[c, position.Column];

                while (s.SquareType == position.SquareControl.SquareType)
                {
                    squares.Add(s.Position);

                    verticalPositionCount++;
                    existsInVertikalPositionLineBonus = existsInVertikalPositionLineBonus || s.LineBonusOrientation == LineBonusOrientation.Vertiсal;
                    existsInVertikalPositionBombBonus = existsInVertikalPositionBombBonus || _newBombBonuses.Exists(n => n.Row == s.Position.Row && n.Column == s.Position.Column);
                    c++;

                    if (c > Constants.GetInstance().PlayingFieldHeight - 1)
                        break;

                    s = _squares[c, position.Column];
                }
            }

            if (verticalPositionCount == 3 && !existsInVertikalPositionLineBonus && !position.SquareControl.LineBonus)
            {
                position.SquareControl.SetLineBonusOrientation(LineBonusOrientation.Vertiсal);
                _newLineBonuses.Add(position);
                _numberOfPoints++;
            }

            if (verticalPositionCount >= 4 && !existsInVertikalPositionBombBonus && !position.SquareControl.BombBonus)
            {
                position.SquareControl.BombBonus = true;
                _newBombBonuses.Add(position);
                _numberOfPoints++;
            }

            if (horizontalPositionCount >= 2 && verticalPositionCount >= 2 && !position.SquareControl.BombBonus)
            {
                position.SquareControl.BombBonus = true;
                _newBombBonuses.Add(position);
                _numberOfPoints++;
            }

            if (squares.Count != 0 && !_newLineBonuses.Contains(position) && !_newBombBonuses.Contains(position))
                squares.Add(position);

            return squares.Where(s => !_newLineBonuses.Contains(s) && !_newBombBonuses.Contains(s)).ToList();
        }

        private async Task MoveDownElementsInColumn(IGrouping<int, SquarePosition> elements)
        {
            var rows = elements.Select(s => s.Row);

            var min = rows.Min();
            var max = rows.Max();

            while(min > 0)
            {
                for(int i = min; i < max + 1; i++)
                {
                    if (_squares[i, elements.Key] != null)
                        continue;

                    await MoveDownSquareOnOnePosition(i, elements.Key);
                }

                min--;
                max--;
            }

            for(int i = max; i >= 0; i--)
            {
                if (_squares[i, elements.Key] != null)
                {
                    if(i != max)
                    {
                        for(int j = i + 1; j < max + 1; j++)
                            await MoveDownSquareOnOnePosition(j, elements.Key);
                    }

                    max--;
                    continue;
                }
            }

            Random rnd = new Random();

            for (int i = max; i >= 0; i--)
            {
                if (_squares[i, elements.Key] == null)
                {
                    var value = rnd.Next() % 5;

                    _squares[0, elements.Key] = GetSquareControlByNumber(value);

                    _squares[0, elements.Key].Location = new Point(elements.Key * Constants.GetInstance().SquareSideSize, -Constants.GetInstance().SquareSideSize);
                    _squares[0, elements.Key].Size = new Size(Constants.GetInstance().SquareSideSize, Constants.GetInstance().SquareSideSize);
                    _squares[0, elements.Key].SetSelectedSquarePanels += SetSelectedSquarePanel;
                    _squares[0, elements.Key].Visible = true;
                    _squares[0, elements.Key].Enabled = false;
                    panel1.Controls.Add(_squares[0, elements.Key]);
                    panel1.Refresh();

                    await MoveDownSquareOnOnePosition(_squares[0, elements.Key]);

                    if (i > 0)
                    {
                        for (int j = 0; j < i; j++)
                            await MoveDownSquareOnOnePosition(j + 1, elements.Key);
                    }
                }
            }
        }

        private async Task MoveDownSquareOnOnePosition(SquareControl movedSquare)
        {
            panel1.Refresh();
            int i = 0;

            while (i < Constants.GetInstance().SquareSideSize / 8)
            {
                movedSquare.Location = new Point(movedSquare.Location.X, movedSquare.Location.Y + 8);
                await Task.Delay(25);
                i++;
            }
        }

        private async Task MoveDownSquareOnOnePosition(int row, int column)
        {
            var movedSquareControl = _squares[row - 1, column];

            await MoveDownSquareOnOnePosition(movedSquareControl);

            _squares[row - 1, column] = null;

            if(movedSquareControl.Position.Row != row || movedSquareControl.Position.Column != column)
            {
                for (int i = 0; i < Constants.GetInstance().PlayingFieldWidth; i++)
                {
                    if (_squares[row, i] == movedSquareControl)
                    {
                        _squares[row, i] = GetSquareControlByNumber((int)movedSquareControl.SquareType - 1);
                        _squares[row, i].Location = new Point(i * Constants.GetInstance().SquareSideSize, row * Constants.GetInstance().SquareSideSize);
                        _squares[row, i].Size = new Size(Constants.GetInstance().SquareSideSize, Constants.GetInstance().SquareSideSize);
                        _squares[row, i].SetSelectedSquarePanels += SetSelectedSquarePanel;
                        _squares[row, i].Visible = true;
                        panel1.Controls.Add(_squares[row, i]);
                    }
                }

                for (int i = 0; i < Constants.GetInstance().PlayingFieldHeight; i++)
                {
                    if (_squares[i, column] == movedSquareControl)
                    {
                        _squares[i, column] = GetSquareControlByNumber((int)movedSquareControl.SquareType - 1);
                        _squares[i, column].Location = new Point(column * Constants.GetInstance().SquareSideSize, i * Constants.GetInstance().SquareSideSize);
                        _squares[i, column].Size = new Size(Constants.GetInstance().SquareSideSize, Constants.GetInstance().SquareSideSize);
                        _squares[i, column].SetSelectedSquarePanels += SetSelectedSquarePanel;
                        _squares[i, column].Visible = true;
                        panel1.Controls.Add(_squares[i, column]);
                    }
                }

                movedSquareControl.Location = new Point(Constants.GetInstance().SquareSideSize * column, Constants.GetInstance().SquareSideSize * row);
            }

            _squares[row, column] = movedSquareControl;
        }

        private async Task DoWhileExistsThreeSquaresLines()
        {
            List<SquarePosition> deletedSquares = new List<SquarePosition>();

            do
            {
                _newLineBonuses = new List<SquarePosition>();
                _newBombBonuses = new List<SquarePosition>();

                deletedSquares = FindAllPositionLines();

                GetAllDeletedWithBonusesSquares(deletedSquares);

                foreach (var n in _newLineBonuses)
                {
                    if (deletedSquares.Contains(n))
                        deletedSquares.Remove(n);
                }

                foreach (var n in _newBombBonuses)
                {
                    if (deletedSquares.Contains(n))
                        deletedSquares.Remove(n);
                }

                DeleteSquareControlsOnPositions(deletedSquares);

                _numberOfPoints = _numberOfPoints + deletedSquares.Count;

                var columnGroups = from d in deletedSquares
                                   group d by d.Column;

                List<Task> tasks = new List<Task>();
                foreach (var group in columnGroups)
                {
                    var task = MoveDownElementsInColumn(group);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            while (deletedSquares.Count > 0);
        }

        private List<SquarePosition> FindAllPositionLines()
        {
            List<SquarePosition> positions = new List<SquarePosition>();

            for(int i = 0; i < Constants.GetInstance().PlayingFieldHeight; i++)
                for(int j = 0; j < Constants.GetInstance().PlayingFieldWidth; j++)
                {
                    var delSquares = GetDeletedSquaresWithPositions(_squares[i, j].Position).Where(s => !positions.Exists(p => s.Row == p.Row && s.Column == p.Column));

                    delSquares = delSquares.Where(d => (!_newBombBonuses.Exists(b => b.Row == d.Row && b.Column == d.Column)) && (!_newLineBonuses.Exists(l => l.Row == d.Row && l.Column == d.Column)));

                    positions.AddRange(delSquares);
                }

            positions = positions.Where(p => (!_newBombBonuses.Exists(b => b.Row == p.Row && b.Column == p.Column)) && (!_newLineBonuses.Exists(l => l.Row == p.Row && l.Column == p.Column))).ToList();
            return positions;
        }

        private void GetAllDeletedWithBonusesSquares(List<SquarePosition> deletedSquares)
        {
            bool addNewDeletedSquares = false;

            do
            {
                addNewDeletedSquares = GetDeletedSquaresWithLineBonus(deletedSquares) || GetDeletedSquaresWithBombBonus(deletedSquares);
            }
            while (addNewDeletedSquares);
        }

        private bool GetDeletedSquaresWithLineBonus(List<SquarePosition> deletedSquares)
        {
            var deletedLineBonuses = deletedSquares.Where(d => d.SquareControl.LineBonus == true && !_newLineBonuses.Contains(d)).ToList();

            bool addNewSquares = false;
            foreach(var d in deletedLineBonuses)
            {
                SquareControl square = null;

                int size = 0;

                if (d.SquareControl.LineBonusOrientation == LineBonusOrientation.Horizontal)
                    size = Constants.GetInstance().PlayingFieldWidth;
                else if (d.SquareControl.LineBonusOrientation == LineBonusOrientation.Vertiсal)
                    size = Constants.GetInstance().PlayingFieldHeight;

                for (int i = 0; i < size; i++)
                {
                    if (d.SquareControl.LineBonusOrientation == LineBonusOrientation.Horizontal)
                        square = _squares[d.Row, i];
                    else if (d.SquareControl.LineBonusOrientation == LineBonusOrientation.Vertiсal)
                        square = _squares[i, d.Column];

                    if (square.Position.Row != d.Row || square.Position.Column != d.Column)
                    {
                        square.SetSquarePicture(Match3.Properties.Resources.Burst);
                        square.Refresh();

                        if (!deletedSquares.Exists(s => s.Row == square.Position.Row && s.Column == square.Position.Column))
                        {
                            deletedSquares.Add(square.Position);
                            addNewSquares = true;
                        }
                    }
                }
            }

            if (addNewSquares)
                GetDeletedSquaresWithLineBonus(deletedSquares);

            if(deletedSquares.Count != 0)
                deletedSquares = deletedSquares.Distinct().ToList();

            return addNewSquares;
        }

        private bool GetDeletedSquaresWithBombBonus(List<SquarePosition> deletedSquares)
        {
            var deletedBombBonuses = deletedSquares.Where(d => d.SquareControl.BombBonus == true && !_newBombBonuses.Contains(d)).ToList();

            bool addNewSquares = false;
            foreach(var d in deletedBombBonuses)
            {
                var findDeletedSquaredWithBombBonuses = WorkBonusForBombSquare(d);

                foreach(var f in findDeletedSquaredWithBombBonuses)
                {
                    if (!deletedSquares.Exists(s => s.Row == f.Row && s.Column == f.Column))
                    {
                        deletedSquares.Add(f);
                        addNewSquares = true;
                    }
                }
            }

            if (addNewSquares)
                GetDeletedSquaresWithBombBonus(deletedSquares);

            if (deletedSquares.Count != 0)
                deletedSquares = deletedSquares.Distinct().ToList();

            return addNewSquares;
        }

        private List<SquarePosition> WorkBonusForBombSquare(SquarePosition bombSquare)
        {
            List<SquarePosition> positions = new List<SquarePosition>();

            if (bombSquare.Column > 0)
            {
                if (bombSquare.Row > 0)
                    positions.Add(_squares[bombSquare.Row - 1, bombSquare.Column - 1].Position);

                positions.Add(_squares[bombSquare.Row, bombSquare.Column - 1].Position);

                if (bombSquare.Row < Constants.GetInstance().PlayingFieldHeight - 1)
                    positions.Add(_squares[bombSquare.Row + 1, bombSquare.Column - 1].Position);
            }

            if (bombSquare.Row > 0)
                positions.Add(_squares[bombSquare.Row - 1, bombSquare.Column].Position);

            if (bombSquare.Row < Constants.GetInstance().PlayingFieldHeight - 1)
                positions.Add(_squares[bombSquare.Row + 1, bombSquare.Column].Position);

            if (bombSquare.Column < Constants.GetInstance().PlayingFieldWidth - 1)
            {
                if (bombSquare.Row > 0)
                    positions.Add(_squares[bombSquare.Row - 1, bombSquare.Column + 1].Position);

                positions.Add(_squares[bombSquare.Row, bombSquare.Column + 1].Position);

                if (bombSquare.Row < Constants.GetInstance().PlayingFieldHeight - 1)
                    positions.Add(_squares[bombSquare.Row + 1, bombSquare.Column + 1].Position);
            }

            bombSquare.SquareControl.SetSquarePicture(Match3.Properties.Resources.Burst);
            bombSquare.SquareControl.Refresh();

            var findSquareInNewBonuses = _newBombBonuses.FirstOrDefault(n => n.Row == bombSquare.Row && n.Column == bombSquare.Column);

            if (findSquareInNewBonuses != null)
                _newBombBonuses.Remove(findSquareInNewBonuses);

            findSquareInNewBonuses = _newLineBonuses.FirstOrDefault(n => n.Row == bombSquare.Row && n.Column == bombSquare.Column);

            if (findSquareInNewBonuses != null)
                _newLineBonuses.Remove(findSquareInNewBonuses);

            Thread.Sleep(250);

            foreach (var p in positions)
            {
                p.SquareControl.SetSquarePicture(Match3.Properties.Resources.Burst);

                var f = _newBombBonuses.FirstOrDefault(n => n.Row == p.Row && n.Column == p.Column);

                if (f != null)
                    _newBombBonuses.Remove(f);

                f = _newLineBonuses.FirstOrDefault(n => n.Row == p.Row && n.Column == p.Column);

                if (f != null)
                    _newLineBonuses.Remove(f);
            }

            positions.Add(bombSquare);

            return positions;
        }
    }
}
