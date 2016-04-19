using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Loki.Bot;
using Loki.Common;

namespace CommunityLib
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public class Maths
    {
        /// <summary>
        /// Supposed to calculate if an entity is in a circle with given radius
        /// Credits to the guy who made it Kappa Keepo
        /// </summary>
        /// <param name="x">Player's position X property</param>
        /// <param name="y">Player's position Y property</param>
        /// <param name="centerX">Actor's position X property</param>
        /// <param name="centerY">Actor's position Y property</param>
        /// <param name="radius">Radius of the circle</param>
        /// <returns></returns>
        private static bool IsInCircle(int x, int y, int centerX, int centerY, int radius)
        {
            var d = (int)Math.Sqrt(Math.Pow(centerX - (float)x, 2) + Math.Pow(centerY - (float)y, 2));
            return d <= radius;
        }

        /// <summary>
        /// Supposed to calculate if an entity is in a circle with given radius
        /// Credits to the guy who made it Kappa Keepo
        /// </summary>
        /// <param name="yourPos">Player's location</param>
        /// <param name="circlePos">Actor's location (center of the circle)</param>
        /// <param name="radius">Radius of the circle</param>
        /// <returns></returns>
        private static bool IsInCircle(Vector2i yourPos, Vector2i circlePos, int radius)
        {
            return IsInCircle(yourPos.X, yourPos.Y, circlePos.X, circlePos.Y, radius);
        }
        
        /// <summary>
        /// Calculate a point (vector) on the border of the circle, based on radius and angle
        /// </summary>
        /// <param name="centerX">Actor's position X property</param>
        /// <param name="centerY">Actor's position Y property</param>
        /// <param name="degree"></param>
        /// <param name="radius">Radius of the circle</param>
        /// <returns></returns>
        private static Vector2i GetPointOnCircle(int centerX, int centerY, int degree, int radius)
        {
            var xOncircle = centerX + (int)(radius * Math.Cos(degree * Math.PI / 180));
            var yOncircle = centerY + (int)(radius * Math.Sin(degree * Math.PI / 180));
            return new Vector2i(xOncircle, yOncircle);
        }

        private static Vector2i GetPointOnCircle(Vector2i center, double radian, double radius)
        {
            return new Vector2i()
            {
                X = center.X + (int)(radius * Math.Cos(radian)),
                Y = center.Y + (int)(radius * Math.Sin(radian))
            };
        }

        private static Vector2 GetAveragePoint(List<Vector2> pts)
        {
            return new Vector2()
            {
                X = pts.Average(p => p.X),
                Y = pts.Average(p => p.Y)
            };
        }
    }
}
