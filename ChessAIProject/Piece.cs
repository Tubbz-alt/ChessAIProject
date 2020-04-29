using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;

namespace ChessAIProject
{
    [Serializable]
    public abstract class Piece
    {
        public string Name { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int LegalX { get; set; }
        public int LegalY { get; set; }
        public Image OriginalImage { get; set; }
        public Image PieceImage { get; set; }
        public Player Player { get; set; }
        public abstract List<Board> GenerateMoves(Board b);
        string PictureURL = "https://cdn5.vectorstock.com/i/1000x1000/15/29/chess-pieces-including-king-queen-rook-pawn-knight-vector-2621529.jpg";
        /// <summary>
        /// Takes the specified location (x1 and y1) from the image and returns it as a smaller image of specified size (x2 and y2)  
        /// </summary>
        /// <param name="x1">Start of image (x)</param>
        /// <param name="y1">Start of image (y)</param>
        /// <param name="x2">End of image (x)</param>
        /// <param name="y2">End of image (y)</param>
        /// <returns></returns>
        protected Image GetImage(int x1, int y1, int x2, int y2)
        {
            var request = WebRequest.Create(PictureURL);
            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                Bitmap b = Bitmap.FromStream(stream) as Bitmap;
                Rectangle rectangle = new Rectangle(x1, y1, x2, y2);
                System.Drawing.Imaging.PixelFormat format = b.PixelFormat;
                Bitmap b2 = b.Clone(rectangle, format);
                //b2.MakeTransparent(Color.White);
                //b2 = new Bitmap(b2, new Size(40, 40));

                return b2 as Image;
            };
        }
        /// <summary>
        /// Validates that pieces are the same type
        /// </summary>
        /// <param name="obj">Thing to type check</param>
        /// <returns></returns>
        public bool ValidMoveType(object obj)
        {
            if (!(obj is Piece)) { return false; }
            if (obj is Empty || this is Empty) { return false; }
            if ((obj as Piece).Player.IsW != Player.IsW) { return false; }
            if (obj is Pawn && this is Pawn) { return true; }
            if (obj is Pawn && this is Queen) { return true; }
            if (obj is King && this is King) { return true; }
            if (obj is Knight && this is Knight) { return true; }
            if (obj is Rook && this is Rook) { return true; }
            if (obj is Queen && this is Queen) { return true; }
            if (obj is Bishop && this is Bishop) { return true; }
            return false;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    public class Pawn : Piece
    {
        public bool enPass, twoStep;
        public Pawn(Player player, int posX, int posY)
        {
            Name = "Pawn"; PosX = posX; PosY = posY; Player = player; twoStep = true; enPass = false;
            if (player.IsW == true) { LegalX = -1; }
            else { LegalX = 1; }
            if (player.IsW == true) { PieceImage = GetImage(85, 100, 200, 200); }
            else { PieceImage = GetImage(85, 385, 200, 200); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            int scale = Player.IsW ? -1 : 1;
            for (int i = scale; Math.Abs(i) <= 2; i += scale)
            {
                for (int ii = -scale; ii != 2 * scale; ii += scale)
                {
                    //Ignore moves outside the board
                    if (PosX + i > 7 || PosY + ii > 7 || PosX + i < 0 || PosY + ii < 0) { continue; }
                    //Ignore destinations with own pieces
                    if (!(b.Pieces[PosX + i, PosY + ii] is Empty) && b.Pieces[PosX + i, PosY + ii].Player.IsW == Player.IsW) { continue; }
                    //If moving sideways
                    if (ii != 0)
                    {
                        //Can't move forward and sideways two steps
                        if (Math.Abs(i) == 2) { continue; }
                        //If an empty square check if can enpassed
                        if (b.Pieces[PosX + i, PosY + ii] is Empty)
                        {
                            //If enpassing remove the enpassed pawn and then add the move
                            if (b.Pieces[PosX, PosY + ii] is Pawn && ((Pawn)b.Pieces[PosX, PosY + ii]).enPass)
                            {
                                var temp = b.Swap(new int[] { PosX, PosY }, new int[] { PosX + i, PosY + ii });
                                temp.Pieces[PosX, PosY + ii] = new Empty(PosX, PosY + ii);
                                temp.RecentMove = new int[] { PosX + i, PosY + ii }; boards.Add(temp); continue;
                            }
                            continue;
                        }
                        //If enemy piece can capture
                        if (b.Pieces[PosX + i, PosY + ii].Player.IsW != Player.IsW) { goto addmove; }
                    }
                    //If it's moving forward one step
                    if (i == scale)
                    {
                        //If there is nothing in front, it's legal
                        if (b.Pieces[PosX + i, PosY + ii] is Empty) { goto addmove; }
                    }
                    //If it's moving twostep, check if it hasn't moved and if interceptings squares are empty
                    if (twoStep && b.Pieces[PosX + (2 * scale), PosY] is Empty && b.Pieces[PosX + scale, PosY] is Empty)
                    //If so, then it's legal
                    { goto addmove; }

                    //Keep it from going to addmove without proper authorization
                    continue;
                addmove:
                    //If the pawn is on the opposite side replace it with a queen
                    // if ((Player.IsW && PosX == 0) || (!Player.IsW && PosX == 7)) {  }
                    boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + i, PosY + ii }));
                }
            }
            return boards;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    class Rook : Piece
    {
        public new int LegalX = 7, LegalY = 7; public bool CanCastle = true;
        public Rook(Player player, int posX, int posY)
        {
            Player = player; PosX = posX; PosY = posY; Name = "Rook";
            if (player.IsW) { PieceImage = GetImage(285, 90, 220, 220); }
            else { PieceImage = GetImage(285, 365, 220, 220); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            //Declare bounds
            var bounds = new List<int[,]>();
            //Right [0]
            bounds.Add(new int[,] { { 0, 7 - PosY }, { 1, 1 } });
            //Left [1]
            bounds.Add(new int[,] { { 0, PosY }, { 1, -1 } });
            //Up [2]
            bounds.Add(new int[,] { { PosX, 0 }, { -1, 1 } });
            //Down [3]
            bounds.Add(new int[,] { { 7 - PosX, 0 }, { 1, 1 } });

            for (int bound = 0; bound < 4; bound++)
            {
                for (int x = 0; x <= bounds[bound][0, 0]; x++)
                {
                    for (int y = 0; y <= bounds[bound][0, 1]; y++)
                    {
                        if (x == 0 && y == 0) { continue; }
                        //If it reaches a piece, it is the furthest in that direction it can move
                        if (!(b.Pieces[PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1])] is Empty))
                        {
                            //If an enemy piece can move there but not further
                            if (b.Pieces[PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1])].Player.IsW != Player.IsW)
                            { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1]) })); }
                            //If an ally piece can't move there, nor further
                            goto next;
                        }
                        //If it's still empty then just add it
                        boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1]) }));
                    }

                }
            next:;
            }
            return boards;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    class Knight : Piece
    {
        public Knight(Player player, int posX, int posY)
        {
            Player = player; PosX = posX; PosY = posY; Name = "Knight";
            if (player.IsW) { PieceImage = GetImage(700, 55, 230, 250); }
            else { PieceImage = GetImage(700, 345, 230, 250); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            //Declare all possible knight moves
            var pairs = new int[8, 2] {
                { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1  }, { 1, -2 }, { 1, 2 }, { -1, 2 }, { -1, -2  }
            };
            for (int i = 0; i <= 7; i++)
            {
                //If the knight is moving off the board it's invalid
                if (PosY + pairs[i, 1] > 7 || PosX + pairs[i, 0] > 7 || PosY + pairs[i, 1] < 0 || PosX + pairs[i, 0] < 0) { continue; }

                //If capturing an enemy or moving to an empty square, it's valid
                if (b.Pieces[PosX + pairs[i, 0], PosY + pairs[i, 1]] is Empty || b.Pieces[PosX + pairs[i, 0], PosY + pairs[i, 1]].Player.IsW != Player.IsW)
                { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + pairs[i, 0], PosY + pairs[i, 1] })); }
            }
            return boards;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    class Bishop : Piece
    {
        //Should use legalx and legaly in the initializer to make move easier
        public new int LegalX = 7, LegalY = 7;
        public Bishop(Player player, int posX, int posY)
        {
            Player = player; PosX = posX; PosY = posY; Name = "Bishop";
            if (player.IsW) { PieceImage = GetImage(485, 55, 240, 240); }
            else { PieceImage = GetImage(485, 340, 240, 240); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            //Format is [0] distance [1] xiterator [2] yiterator
            var bounds = new List<int[]>();
            //Up and to the right [0]
            bounds.Add(new int[] { PosX < 7 - PosY ? PosX : 7 - PosY, -1, 1 });
            //Up and to the left [1]
            bounds.Add(new int[] { PosX < PosY ? PosX : PosY, -1, -1 });
            //Down and to the right [2]
            bounds.Add(new int[] { 7 - PosX < 7 - PosY ? 7 - PosX : 7 - PosY, 1, 1 });
            //Down and to the left [3]
            bounds.Add(new int[] { 7 - PosX < PosY ? 7 - PosX : PosY, 1, -1 });

            //Foreach bound
            for (int bound = 0; bound < 4; bound++)
            {
                //Determine a max walkout distance and path to it
                for (int i = 1; i <= bounds[bound][0]; i++)
                {
                    //Can move onto an empty space
                    if (b.Pieces[PosX + (i * bounds[bound][1]), PosY + (i * bounds[bound][2])] is Empty)
                    { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (i * bounds[bound][1]), PosY + (i * bounds[bound][2]) })); continue; }
                    //Can't move onto one's own piece, or anywhere thereafter
                    if (b.Pieces[PosX + (i * bounds[bound][1]), PosY + (i * bounds[bound][2])].Player.IsW == Player.IsW) { break; }
                    //Can move onto an enemy piece, but not thereafter
                    if (b.Pieces[PosX + (i * bounds[bound][1]), PosY + (i * bounds[bound][2])].Player.IsW != Player.IsW)
                    { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (i * bounds[bound][1]), PosY + (i * bounds[bound][2]) })); break; }
                }
            }
            return boards;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    class Queen : Piece
    {
        public new int LegalX = 7, LegalY = 7;
        public Queen(Player player, int posX, int posY)
        {
            Player = player; PosX = posX; PosY = posY; Name = "Queen";
            if (player.IsW) { PieceImage = GetImage(45, 645, 270, 282); }
            else { PieceImage = GetImage(469, 645, 270, 282); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            //I can't cast queen to another piece type, so I'm just copy-pasting the move-gen code from bishops and rooks

            //Rook

            //Declare bounds
            var bounds = new List<int[,]>();
            //Right [0]
            bounds.Add(new int[,] { { 0, 7 - PosY }, { 1, 1 } });
            //Left [1]
            bounds.Add(new int[,] { { 0, PosY }, { 1, -1 } });
            //Up [2]
            bounds.Add(new int[,] { { PosX, 0 }, { -1, 1 } });
            //Down [3]
            bounds.Add(new int[,] { { 7 - PosX, 0 }, { 1, 1 } });

            for (int bound = 0; bound < 4; bound++)
            {
                for (int x = 0; x <= bounds[bound][0, 0]; x++)
                {
                    for (int y = 0; y <= bounds[bound][0, 1]; y++)
                    {
                        if (x == 0 && y == 0) { continue; }
                        //If it reaches a piece, it is the furthest in that direction it can move
                        if (!(b.Pieces[PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1])] is Empty))
                        {
                            //If an enemy piece can move there but not further
                            if (b.Pieces[PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1])].Player.IsW != Player.IsW)
                            { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1]) })); }
                            //If an ally piece can't move there, nor further
                            goto next;
                        }
                        //If it's still empty then just add it
                        boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1]) }));
                    }

                }
            next:;
            }

            //Bishop

            //Format is [0] distance [1] xiterator [2] yiterator
            var bbounds = new List<int[]>();
            //Up and to the right [0]
            bbounds.Add(new int[] { PosX < 7 - PosY ? PosX : 7 - PosY, -1, 1 });
            //Up and to the left [1]
            bbounds.Add(new int[] { PosX < PosY ? PosX : PosY, -1, -1 });
            //Down and to the right [2]
            bbounds.Add(new int[] { 7 - PosX < 7 - PosY ? 7 - PosX : 7 - PosY, 1, 1 });
            //Down and to the left [3]
            bbounds.Add(new int[] { 7 - PosX < PosY ? 7 - PosX : PosY, 1, -1 });

            //Foreach bound
            for (int bound = 0; bound < 4; bound++)
            {
                //Determine a max walkout distance and path to it
                for (int i = 1; i <= bbounds[bound][0]; i++)
                {
                    //Can move onto an empty space
                    if (b.Pieces[PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2])] is Empty)
                    { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2]) })); continue; }
                    //Can't move onto one's own piece, or anywhere thereafter
                    if (b.Pieces[PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2])].Player.IsW == Player.IsW) { break; }
                    //Can move onto an enemy piece, but not thereafter
                    if (b.Pieces[PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2])].Player.IsW != Player.IsW)
                    { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2]) })); break; }
                }
            }

            return boards;
        }
    }
    /// <summary>
    /// No bugs known
    /// </summary>
    [Serializable]
    class King : Piece
    {
        public new int LegalX = 1, LegalY = 1; public bool CanCastle = true;
        public King(Player player, int posX, int posY)
        {
            Player = player; PosX = posX; PosY = posY; Name = "king"; LegalX = 1; LegalY = 1; CanCastle = true;
            if (player.IsW) { PieceImage = GetImage(251, 615, 270, 330); }
            else { PieceImage = GetImage(677, 615, 270, 330); }
            OriginalImage = Serializer.DeepClone(PieceImage);
        }
        public override List<Board> GenerateMoves(Board b)
        {
            var boards = new List<Board>();
            //If can castle & king is not in check, see if pieces are in the way
            if (CanCastle && !((b.WCheck && Player.IsW == true) || (b.BCheck && Player.IsW == false)))
            {
                if ((b.Pieces[PosX, 7] is Rook) && (b.Pieces[PosX, 7] as Rook).CanCastle)
                {
                    for (int i = 1; i <= 2; i++)
                    {
                        if (!(b.Pieces[PosX, PosY + i] is Empty)) { break; }
                        if (i == 2)
                        {
                            //Add the castled board state
                            var temp = b.Swap(new int[] { PosX, PosY }, new int[] { PosX, 6 })
                                .Swap(new int[] { PosX, 7 }, new int[] { PosX, 5 });
                            temp.RecentMove = new int[] { PosX, 6 };
                            //Even numbered swaps don't change turns
                            temp.WTurn = !temp.WTurn;
                            boards.Add(temp);
                        }
                    }
                }
                if ((b.Pieces[PosX, 0] is Rook) && (b.Pieces[PosX, 0] as Rook).CanCastle)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        if (!(b.Pieces[PosX, PosY - i] is Empty)) { break; }
                        if (i == 3)
                        {
                            //Add the castled board state
                            var temp = b.Swap(new int[] { PosX, PosY }, new int[] { PosX, 2 })
                                .Swap(new int[] { PosX, 0 }, new int[] { PosX, 3 });
                            temp.RecentMove = new int[] { PosX, 2 };
                            //Even numbered swaps don't change turns
                            temp.WTurn = !temp.WTurn;
                            boards.Add(temp);
                        }
                    }
                }
            }
            //Generate standard moves
            for (int i = -1; i <= 1; i++)
            {
                for (int ii = -1; ii <= 1; ii++)
                {
                    //Skip if it goes off the board
                    if (PosX + i > 7 || PosY + ii > 7 || PosX + i < 0 || PosY + ii < 0 || (i == 0 && ii == 0)) { continue; }
                    //If the desired location is empty or an enemy piece, it is [psuedo] legal
                    if (b.Pieces[PosX + i, PosY + ii] is Empty || b.Pieces[PosX + i, PosY + ii].Player.IsW != Player.IsW) { boards.Add(b.Swap(new int[] { PosX, PosY }, new int[] { PosX + i, PosY + ii })); }
                }
            }
            return boards;
        }
        public bool Check(Board b)
        {
            //Knight

            var pairs = new int[8, 2] {
                { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1  }, { 1, -2 }, { 1, 2 }, { -1, 2 }, { -1, -2  }
            };
            for (int i = 0; i <= 7; i++)
            {
                //If the "L" is off the board it's invalid
                if (PosY + pairs[i, 1] > 7 || PosX + pairs[i, 0] > 7 || PosY + pairs[i, 1] < 0 || PosX + pairs[i, 0] < 0) { continue; }

                //If an enemy knight you're in check
                if (b.Pieces[PosX + pairs[i, 0], PosY + pairs[i, 1]] is Knight
                    && b.Pieces[PosX + pairs[i, 0], PosY + pairs[i, 1]].Player.IsW != Player.IsW)
                { return true; }
            }

            //Orthogonal

            //Declare bounds
            var bounds = new List<int[,]>();
            //Right [0]
            bounds.Add(new int[,] { { 0, 7 - PosY }, { 1, 1 } });
            //Left [1]
            bounds.Add(new int[,] { { 0, PosY }, { 1, -1 } });
            //Up [2]
            bounds.Add(new int[,] { { PosX, 0 }, { -1, 1 } });
            //Down [3]
            bounds.Add(new int[,] { { 7 - PosX, 0 }, { 1, 1 } });

            for (int bound = 0; bound < 4; bound++)
            {
                for (int x = 0; x <= bounds[bound][0, 0]; x++)
                {
                    for (int y = 0; y <= bounds[bound][0, 1]; y++)
                    {
                        var p = b.Pieces[PosX + (x * bounds[bound][1, 0]), PosY + (y * bounds[bound][1, 1])];
                        if (x == 0 && y == 0) { continue; }
                        if (!(p is Empty))
                        {
                            if (p.Player.IsW != Player.IsW && (p is Rook || p is Queen || (p is King && (Math.Abs(x) == 1 || Math.Abs(y) == 1))))
                            { return true; }
                            goto next;
                        }

                    }
                }
            next:;
            }

            //Diagonal

            //Format is [0] distance [1] xiterator [2] yiterator
            var bbounds = new List<int[]>();
            //Up and to the right [0]
            bbounds.Add(new int[] { PosX < 7 - PosY ? PosX : 7 - PosY, -1, 1 });
            //Up and to the left [1]
            bbounds.Add(new int[] { PosX < PosY ? PosX : PosY, -1, -1 });
            //Down and to the right [2]
            bbounds.Add(new int[] { 7 - PosX < 7 - PosY ? 7 - PosX : 7 - PosY, 1, 1 });
            //Down and to the left [3]
            bbounds.Add(new int[] { 7 - PosX < PosY ? 7 - PosX : PosY, 1, -1 });

            //Foreach bound
            for (int bound = 0; bound < 4; bound++)
            {
                //Determine a max walkout distance and path to it
                for (int i = 1; i <= bbounds[bound][0]; i++)
                {
                    var p = b.Pieces[PosX + (i * bbounds[bound][1]), PosY + (i * bbounds[bound][2])];
                    //If friendly not in check
                    if (!(p is Empty))
                    {
                        //If friendly not in check
                        if (p.Player.IsW == Player.IsW) { break; }
                        //If an enemy bishop/queen/king then in check
                        if (p is Bishop || p is Queen || (p is King && Math.Abs(i) == 1) || (p is Pawn && p.PosX == PosX - p.LegalX && (p.PosY == PosY - 1 || p.PosY == PosY + 1)))
                        { return true; }
                    }
                }
            }
            return false;
        }
    }
    /// <summary>
    /// Often causes errors when you forget it DOES NOT HAVE A PLAYER! 
    /// Usually occurs when verifying the isW parameter
    /// </summary>
    [Serializable]
    class Empty : Piece
    {
        public Empty(int posX, int posY)
        {
            PosX = posX; PosY = posY; Name = "empty";
        }
        public override List<Board> GenerateMoves(Board b)
        {
            throw new Exception("Can't generate moves for nothing");
        }
    }
}