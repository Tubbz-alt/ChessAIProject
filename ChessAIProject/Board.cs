using System;
using System.Collections.Generic;

namespace ChessAIProject
{
    [Serializable]
    public class Board
    {
        public Player P1 { get; set; }
        public Player P2 { get; set; }
        public Piece[,] Pieces { get; set; }
        public bool WTurn = true;
        public bool WWin = false;
        public bool BWin = false;
        public bool WCheck = false;
        public bool BCheck = false;
        public int[] RecentMove { get; set; }
        public int MoveNumber = 0;
        public Board(Player p1, Player p2, Piece[,] pieces, bool wturn)
        {
            P1 = p1; P2 = p2; Pieces = pieces; WTurn = wturn;
        }
        public Board initBoard()
        {
            Player p1 = P1; Player p2 = P2;
            Piece[,] tempPieces = new Piece[8, 8]
            {
                { new Rook(p2, 0, 0), new Knight(p2, 0, 1), new Bishop(p2, 0, 2), new Queen(p2, 0, 3), new King(p2, 0, 4), new Bishop(p2, 0, 5), new Knight(p2, 0, 6), new Rook(p2, 0, 7) },
                { new Pawn(p2, 1, 0), new Pawn(p2, 1, 1), new Pawn(p2, 1, 2), new Pawn(p2, 1, 3), new Pawn(p2, 1, 4), new Pawn(p2, 1, 5), new Pawn(p2, 1, 6), new Pawn(p2, 1, 7) },
                { new Empty(2, 0), new Empty(2, 1), new Empty(2, 2), new Empty(2, 3), new Empty(2, 4), new Empty(2, 5), new Empty(2, 6), new Empty(2, 7) },
                { new Empty(3, 0), new Empty(3, 1), new Empty(3, 2), new Empty(3, 3), new Empty(3, 4), new Empty(3, 5), new Empty(3, 6), new Empty(3, 7) },
                { new Empty(4, 0), new Empty(4, 1), new Empty(4, 2), new Empty(4, 3), new Empty(4, 4), new Empty(4, 5), new Empty(4, 6), new Empty(4, 7) },
                { new Empty(5, 0), new Empty(5, 1), new Empty(5, 2), new Empty(5, 3), new Empty(5, 4), new Empty(5, 5), new Empty(5, 6), new Empty(5, 7) },
                { new Pawn(p1, 6, 0), new Pawn(p1, 6, 1), new Pawn(p1, 6, 2), new Pawn(p1, 6, 3), new Pawn(p1, 6, 4), new Pawn(p1, 6, 5), new Pawn(p1, 6, 6), new Pawn(p1, 6, 7) },
                { new Rook(p1, 7, 0), new Knight(p1, 7, 1), new Bishop(p1, 7, 2), new Queen(p1, 7, 3), new King(p1, 7, 4), new Bishop(p1, 7, 5), new Knight(p1, 7, 6), new Rook(p1, 7, 7) }
            };
            Pieces = tempPieces;
            return this;
        }
        public Board Swap(int[] start, int[] end)
        {
            //Input verification
            if (Pieces[start[0], start[1]] is Empty) { throw new Exception("Can't swap nothing"); }
            if (!(Pieces[end[0], end[1]] is Empty) && Pieces[start[0], start[1]].Player.IsW == Pieces[end[0], end[1]].Player.IsW)
            { throw new Exception("Can't swap on an own piece"); }
            //Movement
            Board board = Serializer.DeepClone(this);
            board.Pieces[end[0], end[1]] = board.Pieces[start[0], start[1]];
            board.Pieces[end[0], end[1]].PosX = end[0]; board.Pieces[end[0], end[1]].PosY = end[1];
            board.Pieces[start[0], start[1]] = new Empty(start[0], start[1]);
            var p = board.Pieces[end[0], end[1]];
            //Set on first move stuff to false (and enpass to true) for applicable pieces
            if (p is Pawn)
            {
                (p as Pawn).twoStep = false;
                if (Math.Abs(end[0] - start[0]) == 2) { (p as Pawn).enPass = true; }
                //Replace pawn with queen if it's on the opposite side of the board
                if ((end[0] == 0 && p.Player.IsW) || (end[0] == 7 && !p.Player.IsW))
                {
                    board.Pieces[end[0], end[1]] = new Queen(p.Player, end[0], end[1]);
                }
            }
            if (p is King) { (board.Pieces[end[0], end[1]] as King).CanCastle = false; }
            if (p is Rook) { (board.Pieces[end[0], end[1]] as Rook).CanCastle = false; }
            board.WTurn = !board.WTurn;
            board.RecentMove = end;
            return board;
        }
        public List<Board> GenMoveByType(Piece p, bool removeChecks)
        {
            var boards = new List<Board>();
            var v = new List<Board>();

            foreach (Piece p2 in Pieces)
            {
                if (p2 is Empty || p2.Player.IsW != WTurn) { continue; }
                if (p2 is Pawn) { (p2 as Pawn).enPass = false; }
            }

            if (p is Pawn) { v = (p as Pawn).GenerateMoves(this); }
            if (p is King) { v = (p as King).GenerateMoves(this); }
            if (p is Knight) { v = (p as Knight).GenerateMoves(this); }
            if (p is Rook) { v = (p as Rook).GenerateMoves(this); }
            if (p is Queen) { v = (p as Queen).GenerateMoves(this); }
            if (p is Bishop) { v = (p as Bishop).GenerateMoves(this); }

            if (removeChecks)
            {
                foreach (Board b in v)
                {
                    //don't add the board if the king is in check or isn't present
                    bool noking = true;
                    foreach (Piece p2 in b.Pieces)
                    {
                        if (p2 is King && p2.Player.IsW == WTurn)
                        {
                            noking = false;
                            if ((p2 as King).Check(b)) { goto next; }
                        }
                    }
                    if (noking) { goto next; }
                    boards.Add(b);
                next:;
                }
            }
            else
            {
                foreach (Board b in v)
                {
                    boards.Add(b);
                }
            }
            return boards;
        }
        public List<Board> GenMoves(bool removeChecks)
        {
            var boards = new List<Board>();
            foreach (Piece p in Pieces)
            {
                if (p is Empty || p.Player.IsW != WTurn) { continue; }
                if (p is Pawn) { (p as Pawn).enPass = false; }
            }
            foreach (Piece p in Pieces)
            {
                if (p is Empty || p.Player.IsW != WTurn) { continue; }
                //Generates an empty list
                var v = new List<Board>();

                if (p is Pawn) { v = (p as Pawn).GenerateMoves(this); }
                if (p is King) { v = (p as King).GenerateMoves(this); }
                if (p is Knight) { v = (p as Knight).GenerateMoves(this); }
                if (p is Rook) { v = (p as Rook).GenerateMoves(this); }
                if (p is Queen) { v = (p as Queen).GenerateMoves(this); }
                if (p is Bishop) { v = (p as Bishop).GenerateMoves(this); }

                if (removeChecks)
                {
                    foreach (Board b in v)
                    {
                        //don't add the board if the king is in check or isn't present
                        bool noking = true;
                        foreach (Piece p2 in b.Pieces)
                        {
                            if (p2 is King && p2.Player.IsW == WTurn)
                            {
                                noking = false;
                                if ((p2 as King).Check(b)) { goto next; }
                            }
                        }
                        if (noking) { goto next; }
                        boards.Add(b);
                    next:;
                    }
                }
                else
                {
                    foreach (Board b in v)
                    {
                        boards.Add(b);
                    }
                }
            }
            return boards;
        }
    }
}