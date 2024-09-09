using System.Collections.Generic;

namespace Algorithm
{
    public static class PathAlgorithmHelper
    {
        public const int N_COST = 10;
        public const int D_COST = 14;

        public static readonly CoordinateDir[] Arounds = {
            new CoordinateDir(-1, 1, SearchDir.LeftUpper), new CoordinateDir(0, 1, SearchDir.Upper), new CoordinateDir(1, 1, SearchDir.RightUpper),
            new CoordinateDir(1, 0, SearchDir.Right), new CoordinateDir(1, -1, SearchDir.RightLower), new CoordinateDir(0, -1, SearchDir.Lower),
            new CoordinateDir(-1, -1, SearchDir.LeftLower), new CoordinateDir(-1, 0, SearchDir.Left)
        };

        public static readonly CoordinateDir[] DiagonalArounds = {
            new CoordinateDir(-1, 1, SearchDir.LeftUpper), new CoordinateDir(1, 1, SearchDir.RightUpper), new CoordinateDir(1, -1, SearchDir.RightLower), new CoordinateDir(-1, -1, SearchDir.LeftLower)
        };

        public static readonly CoordinateDir[] StraightArounds = {
            new CoordinateDir(0, 1, SearchDir.Upper), new CoordinateDir(1, 0, SearchDir.Right), new CoordinateDir(0, -1, SearchDir.Lower), new CoordinateDir(-1, 0, SearchDir.Left)
        };

        public static readonly Dictionary<SearchDir, CoordinatePair> SearchSteps = new Dictionary<SearchDir, CoordinatePair>() {
            { SearchDir.LeftUpper, new CoordinatePair(new Coordinate(-1, 0), new Coordinate(0, 1)) },
            { SearchDir.RightUpper, new CoordinatePair(new Coordinate(1, 0), new Coordinate(0, 1)) },
            { SearchDir.LeftLower, new CoordinatePair(new Coordinate(-1, 0), new Coordinate(0, -1)) },
            { SearchDir.RightLower, new CoordinatePair(new Coordinate(1, 0), new Coordinate(0, -1)) },
        };

        public enum SearchDir : int
        {
            None = 0,
            Left = 0x0001,
            Right = 0x0010,
            Upper = 0x0100,
            Lower = 0x1000,
            LeftUpper = Left | Upper,
            RightUpper = Right | Upper,
            RightLower = Right | Lower,
            LeftLower = Left | Lower,
            All = LeftUpper | RightLower
        }

        public static SearchDir OppositeDir(this SearchDir dir)
        {
            if (dir == SearchDir.Left) return SearchDir.Right;
            if (dir == SearchDir.Right) return SearchDir.Left;
            if (dir == SearchDir.Upper) return SearchDir.Lower;
            if (dir == SearchDir.Lower) return SearchDir.Upper;
            if (dir == SearchDir.LeftUpper) return SearchDir.RightLower;
            if (dir == SearchDir.RightLower) return SearchDir.LeftUpper;
            if (dir == SearchDir.RightUpper) return SearchDir.LeftLower;
            if (dir == SearchDir.LeftLower) return SearchDir.RightUpper;
            return SearchDir.None;
        }

        public static int CalcG(int fromX, int fromY, int toX, int toY)
        {
            int diffX = System.Math.Abs(toX - fromX);
            int diffY = System.Math.Abs(toY - fromY);
            int min = System.Math.Min(diffX, diffY);
            int max = System.Math.Max(diffX, diffY);
            return D_COST * min + N_COST * (max - min);
        }

        public static int CalcH(int fromX, int fromY, int toX, int toY)
        {
            int diffX = System.Math.Abs(toX - fromX);
            int diffY = System.Math.Abs(toY - fromY);
            return (diffX + diffY) * N_COST;
        }

        public static Node MakeNode(int x, int y, int beforeX, int beforeY, int toX, int toY)
        {
            Node node = new Node();
            node.x = x;
            node.y = y;
            node.parentX = beforeX;
            node.parentY = beforeY;
            node.g = CalcG(beforeX, beforeY, x, y);
            node.h = CalcH(x, y, toX, toY);
            node.f = node.g + node.h;
            return node;
        }

        public static bool IsOutOfRange(int x, int y, int xCount, int yCount)
        {
            return x < 0 || y < 0 || x >= xCount || y >= yCount;
        }

    }

    public struct Node
    {
        public int x, y;
        public int parentX, parentY;
        public int g, h, f;

        public override bool Equals(object obj)
        {
            if (obj is Node)
            {
                Node node = (Node)obj;
                return node.x == x && node.y == y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(x, y, parentX, parentY, g, h, f);
        }
    }

    public struct CoordinateDir
    {
        public int x, y;
        public PathAlgorithmHelper.SearchDir dir;

        public CoordinateDir(int x, int y, PathAlgorithmHelper.SearchDir dir)
        {
            this.x = x;
            this.y = y;
            this.dir = dir;
        }

        public override bool Equals(object obj)
        {
            if (obj is Coordinate)
            {
                Coordinate coord = (Coordinate)obj;
                return coord.x == x && coord.y == y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(x, y);
        }
    }

    public struct Coordinate
    {
        public int x, y;

        public Coordinate(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Coordinate)
            {
                Coordinate coord = (Coordinate)obj;
                return coord.x == x && coord.y == y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(x, y);
        }
    }

    public struct CoordinatePair
    {
        public Coordinate horizontal;
        public Coordinate vertical;

        public CoordinatePair(Coordinate horizontal, Coordinate vertical)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }
    }
}