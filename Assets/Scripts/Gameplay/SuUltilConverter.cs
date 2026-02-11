using System;
using UnityEngine;
public static class SuUltilConverter
{
    //private const string heart = "Heart";
    //private const string gold = "Gold";
    //private const string gift = "Gift";
    //private const string scissors = "Scissors";
    //private const string ticket = "Ticket";
    //private const string shuffle = "Shuffle";
    //private const string freeze = "Freeze";
    public static string FormatRewardValue(ItemInfo item)
    {
        if(item.type == ItemType.Coin)
            return item.value.ToString();
        if (item.type != ItemType.Heart)
            return $"x{item.value}";

        return FormatTimeInMinutes(item.value);
    }
    

    public static string FormatTimeInMinutes(int minutes)
    {
        if (minutes < 60) return $"{minutes}m";
        int hours = minutes / 60;
        int remMins = minutes % 60;
        return remMins == 0 ? $"{hours}h" : $"{hours}h{remMins}m";
    }
    public static string SecondsToTimeString(double totalSeconds)
    {
        if (totalSeconds <= 0)
            return "00:00";

        TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

        if (timeSpan.Days >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h";

        if (timeSpan.Hours >= 1)
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public static DateTime StringToDateTime(string date)
    {
        if (DateTime.TryParse(date, out var parsedDate))
        {
            return parsedDate;
        }    

        return DateTime.Now;
    }

    

}
