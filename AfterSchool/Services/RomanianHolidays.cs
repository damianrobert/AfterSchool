namespace AfterSchool.Services;

public static class RomanianHolidays
{
    /// <summary>
    /// Returns all Romanian public holidays for the given year.
    /// Includes fixed national holidays and variable Orthodox Easter-based holidays.
    /// </summary>
    public static Dictionary<DateTime, (string En, string Ro)> GetHolidays(int year)
    {
        var h = new Dictionary<DateTime, (string, string)>();
        void Add(DateTime d, string en, string ro) => h[d.Date] = (en, ro);

        // Fixed national holidays
        Add(new DateTime(year,  1,  1), "New Year's Day",              "Anul Nou");
        Add(new DateTime(year,  1,  2), "New Year's Day",              "Anul Nou");
        Add(new DateTime(year,  1, 24), "Union Day",                   "Ziua Unirii");
        Add(new DateTime(year,  5,  1), "Labor Day",                   "Ziua Muncii");
        Add(new DateTime(year,  6,  1), "Children's Day",              "Ziua Copilului");
        Add(new DateTime(year,  8, 15), "Assumption of Mary",          "Adormirea Maicii Domnului");
        Add(new DateTime(year, 11, 30), "St. Andrew's Day",            "Sfântul Andrei");
        Add(new DateTime(year, 12,  1), "National Day",                "Ziua Națională");
        Add(new DateTime(year, 12, 25), "Christmas Day",               "Crăciun");
        Add(new DateTime(year, 12, 26), "Second Day of Christmas",     "A doua zi de Crăciun");

        // Variable Orthodox Easter-based holidays
        var easter = OrthodoxEaster(year);
        Add(easter.AddDays(-2), "Good Friday",   "Vinerea Mare");
        Add(easter,             "Easter Sunday", "Paște");
        Add(easter.AddDays(1),  "Easter Monday", "Lunea Paștelui");
        Add(easter.AddDays(49), "Whit Sunday",   "Rusalii");
        Add(easter.AddDays(50), "Whit Monday",   "Lunea Rusaliilor");

        return h;
    }

    // Julian calendar Easter algorithm converted to Gregorian (+13 days, valid 1900–2099).
    private static DateTime OrthodoxEaster(int year)
    {
        int a = year % 4;
        int b = year % 7;
        int c = year % 19;
        int d = (19 * c + 15) % 30;
        int e = (2 * a + 4 * b - d + 34) % 7;
        int month = (d + e + 114) / 31;
        int day   = (d + e + 114) % 31 + 1;
        return new DateTime(year, month, day).AddDays(13);
    }
}
