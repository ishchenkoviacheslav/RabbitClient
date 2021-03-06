﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharedClasses.DTO
{
    [Serializable]
    public class Coordinate
    {
        /// <summary>
        /// X and Y coordinate
        /// </summary>
        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
