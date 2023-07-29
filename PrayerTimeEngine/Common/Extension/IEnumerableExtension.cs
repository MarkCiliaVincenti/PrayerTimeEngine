﻿namespace PrayerTimeEngine.Common.Extension
{
    public static class IEnumerableExtension
    {
        public static List<T> ToSingleElementLst<T>(this T obj)
        {
            return new List<T>() { obj };
        }
    }
}