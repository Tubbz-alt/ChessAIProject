using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChessAIProject
{
    class IO
    {
        static readonly string BasePath = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
        public static bool Running = false;
        public static bool WWon = false;
        public static NN Read(int num)
        {
            NN nn = new NN();
            if (Running) { throw new Exception("Already accessing file"); }
            Running = true;
            var fs = new FileStream(BasePath + "\\WBs\\" + num.ToString() + ".txt", FileMode.Open, FileAccess.Read, FileShare.None);
            var sr = new StreamReader(fs);
            string text = sr.ReadToEnd();
            sr.Close(); fs.Close();
            string[] split = text.Split(' ');

            int numlayers = int.Parse(split[0]);
            nn.Layers = new List<Layer>();

            int iterator = 1;
            for (int j = 0; j < numlayers; j++)
            {
                int length = int.Parse(split[iterator]); iterator++;
                int inputlength = int.Parse(split[iterator]); iterator++;
                nn.Layers.Add(new Layer(length, inputlength));
                for (int i = 0; i < length; i++)
                {
                    for (int ii = 0; ii < inputlength; ii++)
                    {
                        nn.Layers[j].Weights[i, ii] = double.Parse(split[iterator]);
                        iterator++;
                    }
                    nn.Layers[j].Biases[i] = double.Parse(split[iterator]);
                    iterator++;
                }
            }
            Running = false;
            return nn;
        }
        public static void Write(NN nn, int num)
        {
            var fs = new FileStream(BasePath + "\\WBs\\" + num.ToString() + ".txt", FileMode.Create, FileAccess.Write, FileShare.None);
            var sw = new StreamWriter(fs);
            sw.Write(nn.NumLayers + " ");
            foreach (Layer l in nn.Layers)
            {
                sw.Write(l.Length + " " + l.InputLength + " ");
                for (int i = 0; i < l.Length; i++)
                {
                    for (int ii = 0; ii < l.InputLength; ii++)
                    {
                        sw.Write(l.Weights[i, ii] + " ");
                    }
                    sw.Write(l.Biases[i] + " ");
                }
            }
            sw.Close(); fs.Close();
        }
        public static List<Board> ReadGame(int num)
        {
            var fs = new FileStream(BasePath + "\\Games.csv", FileMode.Open, FileAccess.Read, FileShare.None);
            var sr = new StreamReader(fs);
            var boards = new List<Board>();
            List<char> PieceChars = new List<char> { 'n', 'b', 'r', 'q', 'k' };

            //Can't use fs.Position b/c games are varied in length
            //This manually sets the game to "num"
            for (int i = 0; i < num; i++) { sr.ReadLine(); }
            string[] text = sr.ReadLine().Split(',');
            if (text[6] == "white") { WWon = true; } else { WWon = false; }
            string[] game = text[12].Split(' ');
            var board = new Board(new Player(true), new Player(false), new Piece[8,8], true).initBoard();
            for (int i = 0; i < game.Length; i++)
            {
                var ca = game[i].ToCharArray();
                int[] location = null;
                //If a castle
                if (char.ToLower(ca[0]) == 'o') {
                    int count = 0;
                    foreach (char c in ca) { if (char.ToLower(c) == 'o') { count++; } }
                    //1 is king, 2 is rook (default is black queenside castle)
                    int x1 = 0, x2 = 0, y1 = 4, y2 = 0;
                    //If white
                    if (i % 2 != 0) { x1 = 7; x2 = 7; } 
                    //If kingside
                    if (count % 2 != 0) { y2 = 7; }
                    board.Swap(new int[] { x1, y1 }, new int[] { x2, y2 });
                    boards.Add(board);
                    continue;
                }
                //If not a castle determine location of piece
                else
                {
                    location = movelocation(game[i]);
                }
                //If a pawn
                if (!(PieceChars.Contains(char.ToLower(ca[0])))) 
                { var _ = genmove(new Pawn(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); continue; }
              
                //If another piece
                switch (char.ToLower(ca[0]))
                {
                    //knight
                    case 'n': var _ = genmove(new Knight(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); break;
                    //bishop
                    case 'b': _ = genmove(new Bishop(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); break;
                    //rook
                    case 'r': _ = genmove(new Rook(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); break;
                    //queen
                    case 'q': _ = genmove(new Queen(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); break;
                    //king
                    case 'k': _ = genmove(new King(new Player(i % 2 == 0), 0, 0), board, location, i % 2 == 0); board = _; boards.Add(_); break;
                }
            }
            sr.Close(); fs.Close();
            return boards;

            Board genmove(Piece type, Board original, int[] destination, bool wturn)
            {
                var possibilities = new List<Board>();
                foreach (Piece p in original.Pieces)
                {
                    if (p is Empty) { continue; }
                    if (p.Player.IsW != wturn) { continue; }
                    if (!type.ValidMoveType(p)) { continue; }
                    foreach (Board b in p.GenerateMoves(original))  { possibilities.Add(b); }
                }
                foreach (Board b in possibilities)
                {
                    if (type.ValidMoveType(b.Pieces[destination[0], destination[1]]))
                    {
                        return b;
                    }
                }
                return null;
            }

            int[] movelocation(string move)
            {
                for (int i = 0; i < move.Length; i++)
                {
                    if (int.TryParse(move[i].ToString(), out int result)) 
                    {
                        int prior = -1;
                        switch (char.ToLower(move[i - 1]))
                        {
                            case 'a': prior = 0; break;
                            case 'b': prior = 1; break;
                            case 'c': prior = 2; break;
                            case 'd': prior = 3; break;
                            case 'e': prior = 4; break;
                            case 'f': prior = 5; break;
                            case 'g': prior = 6; break;
                            case 'h': prior = 7; break;
                        }
                        return new int[] { 8 - result, prior };
                    }
                }
                return null;
            }
        }
    }
}
