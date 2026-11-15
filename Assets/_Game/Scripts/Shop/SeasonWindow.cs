using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SeasonDef
{
    [SerializeField] private string id;
    [SerializeField] private string name;
    [SerializeField] private int startMonth = 12;
    [SerializeField] private int startDay = 15;
    [SerializeField] private int endMonth = 1;
    [SerializeField] private int endDay = 15;
    [SerializeField] private List<CosmeticItem> items = new List<CosmeticItem>();

    public SeasonDef(string id, string name, int startMonth, int startDay, int endMonth, int endDay, List<CosmeticItem> items)
    {
        this.id = id;
        this.name = name;
        this.startMonth = startMonth;
        this.startDay = startDay;
        this.endMonth = endMonth;
        this.endDay = endDay;
        this.items = items ?? new List<CosmeticItem>();
    }

    public string Id => id;
    public string Name => name;
    public string Title => Loc.T(name);
    public int StartMonth => startMonth;
    public int StartDay => startDay;
    public int EndMonth => endMonth;
    public int EndDay => endDay;
    public IReadOnlyList<CosmeticItem> Items => items;
}

public static class SeasonWindow
{
    public static bool IsOpen(SeasonDef season, DateTime local)
    {
        return DaysLeft(season, local) > 0;
    }

    public static int DaysLeft(SeasonDef season, DateTime local)
    {
        if (season == null) return 0;

        var today = local.Date;
        bool wraps = season.EndMonth < season.StartMonth || (season.EndMonth == season.StartMonth && season.EndDay < season.StartDay);
        for (int year = today.Year - 1; year <= today.Year; year++)
        {
            var start = Make(year, season.StartMonth, season.StartDay);
            var end = Make(wraps ? year + 1 : year, season.EndMonth, season.EndDay);
            if (today >= start && today <= end) return (end - today).Days + 1;
        }
        return 0;
    }

    public static SeasonDef Current(IReadOnlyList<SeasonDef> seasons, DateTime local)
    {
        if (seasons == null) return null;
        foreach (var season in seasons)
            if (IsOpen(season, local)) return season;
        return null;
    }

    private static DateTime Make(int year, int month, int day)
    {
        month = Mathf.Clamp(month, 1, 12);
        day = Mathf.Clamp(day, 1, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day);
    }
}
