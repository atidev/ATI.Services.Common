using System;
using System.Collections.Generic;
using System.Linq;


namespace ATI.Services.Common.Extensions
{
   public static class ListExtensions
    {
        public static T RandomItem<T>(this IList<T> list)
        {
            int count = list.Count;
            if (count == 0)
                return default;
            int num = Math.Abs(Guid.NewGuid().GetHashCode());
            return list.ElementAt(num % count);
        }
    }
}
