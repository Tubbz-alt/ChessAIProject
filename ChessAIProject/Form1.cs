using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ChessAIProject
{
    public partial class Form1 : Form
    {
        List<int[]> PossibleMoves { get; set; }
        int[] SelectedPiece { get; set; }

        private Panel[,] BoardPanel = new Panel[8, 8];
        private Button[] Buttons = new Button[4];
        private NN ActiveNN { get; set; }
        Board activeboard;
        public Board ActiveBoard 
        {
            get { return activeboard; } 
            set {
                activeboard = value; PanelUpdate(true);
                if (VsAI[0] && VsAI[1] == activeboard.WTurn)
                { new Task(() => { ActiveBoard = ActiveNN.Move(activeboard); }).Start(); }
            } 
        }
        public List<Board> PossibleBoards { get; set; }
        bool Training = false;
        private int tilesize;
        double ImageScale = 1;
        double LastScale = 0;
        bool ResetNN = true;
        int SaveEveryX = 1;
        int MaxMoves = 50;
        bool[] VsAI = new bool[3];
        private int[] formsize { get; set; }
        public Form1()
        {
            ActiveBoard = new Board(new Player(true), new Player(false), new Piece[8, 8], true).initBoard();
            ActiveNN = new NN();

            if (ResetNN) { ActiveNN.Init(); }
            else { new Task(() => { ActiveNN = IO.Read(0); }).Start(); }
           
            MaximizeBox = false;
            InitializeComponent();
            Text = "NotJustChess";
            Icon = new Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JCPawn.Ico"));
            //"C:\Users\gwflu\source\repos\JustChess\JustChess\JCPawn.PNG"
            for (int i = 0; i < 8; i++)
            {
                for (int ii = 0; ii < 8; ii++)
                {
                    var newPanel = new Panel
                    {
                        Size = new Size(tilesize, tilesize),
                        Location = new Point(tilesize * i, tilesize * ii)
                    };

                    newPanel.Click += newPanel_Click;
                    newPanel.BackgroundImageLayout = ImageLayout.Center;
                    if (!(ActiveBoard.Pieces[ii, i] is Empty))
                    {
                        newPanel.BackgroundImage = ScaleImage(ActiveBoard.Pieces[ii, i].PieceImage, 40);
                        newPanel.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
                    }

                    //Add panel to controls
                    Controls.Add(newPanel);
                    //Add panel to board
                    BoardPanel[i, ii] = newPanel;

                    //Color the board
                    if (i % 2 == 0)
                        newPanel.BackColor = ii % 2 != 0 ? Color.Black : Color.White;
                    else
                        newPanel.BackColor = ii % 2 != 0 ? Color.White : Color.Black;
                }
            }
            Button button = new Button
            {
                Size = new Size(tilesize * 2, tilesize / 2),
                Location = new Point(tilesize * 2, tilesize * 8),
                Text = "Reset"
            };
            Buttons[0] = button;
            button.Click += Button_Click;
            Controls.Add(button);
            Button button2 = new Button
            {
                Size = new Size(tilesize * 2, tilesize / 2),
                Location = new Point(tilesize * 4, tilesize * 8),
                Text = "Train"
            };
            Buttons[1] = button2;
            button2.Click += Button2_Click;
            Controls.Add(button2);
            Button button3 = new Button
            {
                Size = new Size(tilesize * 2, tilesize / 2),
                Location = new Point(tilesize * 6, tilesize * 8),
                Text = "AIvsAI"
            };
            Buttons[2] = button3;
            button3.Click += Button3_Click;
            Controls.Add(button3);
            Button button4 = new Button
            {
                Size = new Size(tilesize * 2, tilesize / 2),
                Location = new Point(0, tilesize * 8),
                Text = "Vs AI"
            };
            Buttons[3] = button4;
            button4.Click += Button4_Click;
            Controls.Add(button4);


            //Set dynamic scaling
            this.ResizeEnd += Form1_Resize;
            formsize = new int[] { 0, 0 };
            Size = new Size(500, 500);
            Form1_Resize(this, new EventArgs());
        }
        private Image ScaleImage(Image original, int size)
        {
            if (original is null) { return null; }
            Bitmap b2 = new Bitmap(original, new Size(size, size));
            for (int i = 0; i < b2.Height; i++)
            {
                for (int ii = 0; ii < b2.Width; ii++)
                {
                    Color pixelColor = b2.GetPixel(i, ii);
                    if (pixelColor.GetBrightness() > .5) { b2.SetPixel(i, ii, Color.Transparent); }
                    else { b2.SetPixel(i, ii, Color.HotPink); }
                }
            }
            return b2;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (Size.Width == formsize[0] && Size.Height == formsize[1]) { return; }
            Control control = (Control)sender;
            tilesize = control.Size.Width / 9;
            // Set form dimentions
            control.Size = new Size(control.Size.Width - (tilesize / 2), control.Size.Width + (tilesize / 2));
            //Assuming the form is >16 px resize the board
            if (control.Size.Width > 16)
            {
                int fontsize = 8; ImageScale = 1;
                if (tilesize < 40) { fontsize = 4; ImageScale = .5; }
                if (tilesize > 60) { fontsize = 12; ImageScale = 1.4; }
                if (tilesize > 75) { fontsize = 16; ImageScale = 2; }

                for (int i = 0; i < 8; i++)
                {
                    for (int ii = 0; ii < 8; ii++)
                    {
                        BoardPanel[i, ii].Size = new Size(tilesize, tilesize);
                        BoardPanel[i, ii].Location = new Point(tilesize * i, tilesize * ii);
                    }
                }
                if (LastScale != ImageScale) { PanelUpdate(); }

                //TODO: put this in a loop

                Buttons[0].Size = new Size(tilesize * 2, tilesize / 2);
                Buttons[0].Location = new Point(tilesize * 2, tilesize * 8);
                Buttons[0].Font = new Font("Tahoma", fontsize);
                Buttons[1].Size = new Size(tilesize * 2, tilesize / 2);
                Buttons[1].Location = new Point(tilesize * 4, tilesize * 8);
                Buttons[1].Font = new Font("Tahoma", fontsize);
                Buttons[2].Size = new Size(tilesize * 2, tilesize / 2);
                Buttons[2].Location = new Point(tilesize * 6, tilesize * 8);
                Buttons[2].Font = new Font("Tahoma", fontsize);
                Buttons[3].Size = new Size(tilesize * 2, tilesize / 2);
                Buttons[3].Location = new Point(0, tilesize * 8);
                Buttons[3].Font = new Font("Tahoma", fontsize);
            }
            formsize[0] = Serializer.DeepClone(Size.Width);
            formsize[1] = Serializer.DeepClone(Size.Height);
        }
        private void Button_Click(object sender, EventArgs e)
        {
            SelectedPiece = null; PossibleMoves = null;
            new Task(() => { PanelUpdate(); }).Start();
            ActiveBoard = new Board(new Player(true), new Player(false), new Piece[8, 8], true).initBoard();
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            //This button is a toggle (prevents clicking before ready again, as well)
            if (Training) { Training = false; Buttons[1].Enabled = false; return; }
            Training = true;
            //Need to check number of games
            //Index 0 is file format
            var thread =
                    new Thread(() =>
                    {

                        //TODO: find actual number of games available

                        for (int j = 1; j < 10000; j++)
                        {
                            while (Training)
                            {
                                var moves = IO.ReadGame(j);
                                for (int c = 0; c < moves.Count; c++)
                                {
                                    //Whatever the player did is the right move
                                    var possibilities = new List<Board>();
                                    //Generate moves from the same board as the players
                                    if (c != 0) { possibilities = moves[c - 1].GenMoves(true); }
                                    //Generate moves from a fresh board if none exists prior in array
                                    else { possibilities = (new Board(new Player(true), new Player(false), new Piece[8, 8], true).initBoard().GenMoves(true)); }
                                    //Translate board to numbers
                                    var doubleboard = eval(moves[c], c % 2 == 0);
                                    //Foreach move the player could have made, evaluate it in relation to their actual move
                                    foreach (Board b in possibilities)
                                    {
                                        //The player's move was the right one
                                        if (b.RecentMove[0] == moves[c].RecentMove[0] && b.RecentMove[1] == moves[c].RecentMove[1])
                                        //Calculation and backpropegation of error
                                        { ActiveNN.Run(ActivationFunctions.Normalize(doubleboard, 8, 8), 1, false); }
                                        //Other moves are not
                                        else { ActiveNN.Run(ActivationFunctions.Normalize(doubleboard, 8, 8), 0, false); }
                                    }

                                    //TODO: add feedback for evaluating player board states (one of the players won after all)

                                }
                                //Batch descent
                                ActiveNN.Run(moves.Count);
                                if (j % SaveEveryX == 0) { new Task(() => { IO.Write(ActiveNN, 0); }).Start(); }
                                //Enable button in case it was disabled before continuing
                                Invoke(new Action(() => { Buttons[1].Enabled = true; }));
                            }
                        }
                    });
            thread.IsBackground = true;
            thread.Start();
           
            double[,] eval(Board move, bool isw)
            {
                var input = new double[8, 8];
                for (int i = 0; i < 8; i++)
                {
                    for (int ii = 0; ii < 8; ii++)
                    {
                        //Set piece values equal to standard chess piece values
                        Piece p = move.Pieces[i, ii];
                        //Don't have to set empty piece = 0 b/c array initialization does it automatically
                        if (p is Empty) { continue; }
                        if (p is Pawn) { input[i, ii] = 1d; }
                        if (p is Knight || p is Bishop) { input[i, ii] = 3d; }
                        if (p is Rook) { input[i, ii] = 5d; }
                        if (p is Queen) { input[i, ii] = 9d; }
                        if (p is King) { input[i, ii] = 15d; }

                        //Set opposite color piece values to negative
                        if (p.Player.IsW != isw) { input[i, ii] *= -1; }
                    }
                }
                return input;
            }
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            VsAI[0] = false;
            if (VsAI[2]) { VsAI[2] = false; Buttons[2].Enabled = false; return; }
            VsAI[2] = true;

            SelectedPiece = null; PossibleMoves = null;
            new Task(() => { PanelUpdate(); }).Start();

            var nn1 = new NN();
            var nn2 = new NN();

            if (ResetNN) { nn1.Init(); nn2.Init(); }
            else { nn1 = IO.Read(0); nn2 = IO.Read(0); }
            nn1 = nn1.SetColor(true); nn2 = nn2.SetColor(false);

            var thread =
            new Thread(() =>
            {
                Board Compitition = ActiveBoard;
                int movecount = 0;
                //Compete until a victor is decided or movecount is exceeded
                do
                {
                    if (nn1.player.IsW == Compitition.WTurn) { Compitition = nn1.Move(Compitition); }
                    else { Compitition = nn2.Move(Compitition); }

                    Invoke((Action)delegate { ActiveBoard = Compitition; Buttons[2].Enabled = true; });
                    movecount++;
                }
                while (VsAI[2] && !Compitition.WWin && !Compitition.BWin && movecount < MaxMoves);
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            VsAI[2] = false;
            var b = sender as Button;
            string text = "Invalid";
            if (b.Text == "Vs AI") { text = "White AI"; VsAI[0] = true; VsAI[1] = true; }
            if (b.Text == "White AI") { text = "Black AI"; VsAI[0] = true; VsAI[1] = false; }
            if (b.Text == "Black AI") { text = "Vs AI"; VsAI[0] = false; }
            b.Text = text;
            if (VsAI[0] && VsAI[1] == ActiveBoard.WTurn) 
            {
                SelectedPiece = null; PossibleMoves = null; 
                new Task(() => { PanelUpdate(); }).Start();
                new Task(() => { ActiveBoard = ActiveNN.Move(activeboard); }).Start();
            }
        }
        void UpdateImages(Board compare)
        {
            for (int j = 0; j < 8; j++)
            {
                for (int jj = 0; jj < 8; jj++)
                {
                    if (ActiveBoard.Pieces[j, jj] != compare.Pieces[j, jj])
                    {
                        BoardPanel[jj, j].BackgroundImage = ScaleImage(compare.Pieces[j, jj].PieceImage, (int)(40 * ImageScale));
                    }
                }
            }
        }
        void newPanel_Click(object sender, EventArgs e)
        {
            if (VsAI[2] || ( VsAI[0] && VsAI[1] == ActiveBoard.WTurn)) { return; }
            Panel pan = sender as Panel;
            if (pan is null) { return; }
            Piece pic = ActiveBoard.Pieces[pan.Location.Y / tilesize, pan.Location.X / tilesize];
            //Select piece if valid
            if (!(pic is Empty) && pic.Player.IsW == ActiveBoard.WTurn)
            {
                SelectedPiece = new int[] { pic.PosX, pic.PosY };
                PossibleBoards = ActiveBoard.GenMoveByType(pic, true);
                PossibleMoves = new List<int[]>();
                foreach (Board b in PossibleBoards)
                {
                    PossibleMoves.Add(new int[] { b.RecentMove[0], b.RecentMove[1] });
                }
                PanelUpdate();
                return;
            }
            //Move if valid
            if (SelectedPiece != null)
            {
                for (int i = 0; i < PossibleMoves.Count; i++)
                {
                    if ((pic.PosX != PossibleMoves[i][0]) || (pic.PosY != PossibleMoves[i][1])) { continue; }
                    //Only update panels as needed
                    UpdateImages(PossibleBoards[i]);
                    SelectedPiece = null;
                    PossibleMoves = null;
                    ActiveBoard = PossibleBoards[i];
                    break;
                }
                return;
            }
        }
        void PanelUpdate() { PanelUpdate(false); }
        void PanelUpdate(bool updateimages)
        {
            if (BoardPanel[0, 0] is null) { return; }
            bool resize = false;
            if (LastScale != ImageScale) { resize = true; LastScale = ImageScale; }
            for (int i = 0; i < 8; i++)
            {
                for (int ii = 0; ii < 8; ii++)
                {
                    var p = BoardPanel[i, ii];
                    if (i % 2 == 0)
                        p.BackColor = ii % 2 != 0 ? Color.Black : Color.White;
                    else
                        p.BackColor = ii % 2 != 0 ? Color.White : Color.Black;
                    if (resize || updateimages)
                    {
                        p.BackgroundImage = ScaleImage(ActiveBoard.Pieces[ii, i].PieceImage, (int)(40 * ImageScale));
                    }
                }
            }
            if (SelectedPiece != null) { BoardPanel[SelectedPiece[1], SelectedPiece[0]].BackColor = Color.BlanchedAlmond; }
            if (PossibleMoves != null) { foreach (int[] i in PossibleMoves) { BoardPanel[i[1], i[0]].BackColor = Color.Orange; } }
        }
    }
}