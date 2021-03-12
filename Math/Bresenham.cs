using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
	public static class Bresenham
	{
		private static List<Vector2I> _sharedResultList = new List<Vector2I>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SwapInts(ref int i, ref int t)
		{
			var temp = i;
			i = t;
			t = temp;
		}

		public static List<Vector2I> GetLinePoints(Vector2I start, Vector2I end)
		{
			_sharedResultList.Clear();

			bool isSteep = Math.Abs(end.Y - start.Y) > Math.Abs(end.X - start.X);

			if (isSteep)
			{
				SwapInts(ref start.X, ref start.Y);
				SwapInts(ref end.X, ref end.Y);
			}

			if (start.X > end.X)
			{
				SwapInts(ref start.X, ref end.X);
				SwapInts(ref start.Y, ref end.Y);
			}

			int diffX = (end.X - start.X);
			int diffY = Math.Abs(end.Y - start.Y);
			int error = (diffX / 2);
			int yStep = (start.Y < end.Y ? 1 : -1);
			int y = start.Y;

			for (int x = start.X; x <= end.X; ++x)
			{
				if (isSteep)
					_sharedResultList.Add(new Vector2I(y, x));
				else
					_sharedResultList.Add(new Vector2I(x, y));

				error = error - diffY;

				if (error < 0)
				{
					y += yStep;
					error += diffX;
				}
			}

			return _sharedResultList;
		} // GetLinePoints
	} // Bresenham
}
