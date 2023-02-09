using System.Collections.Generic;
using System.Linq;

namespace ModTerminal
{
    internal class ValuesLastComparer<T> : Comparer<T>
    {
        private readonly T[] values;
        public ValuesLastComparer(params T[] values) 
        {
            this.values = values;
        }

        public override int Compare(T x, T y)
        {
            int xValue = values.Contains(x) ? 1 : 0;
            int yValue = values.Contains(y) ? 1 : 0;

            return xValue - yValue;
        }
    }
}
