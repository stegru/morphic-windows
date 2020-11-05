﻿namespace Morphic.Settings.SolutionsRegistry
{

    public class SettingId
    {
        public static readonly SettingId ColorFiltersEnabled = new SettingId("com.microsoft.windows.colorFilters", "enabled");
        public static readonly SettingId ColorFiltersFilterType = new SettingId("com.microsoft.windows.colorFilters", "filterType");
        public static readonly SettingId HighContrastEnabled = new SettingId("com.microsoft.windows.highContrast", "enabled");
        public static readonly SettingId NarratorEnabled = new SettingId("com.microsoft.windows.narrator", "enabled");
        public static readonly SettingId LightThemeApps = new SettingId("com.microsoft.windows.lightTheme", "apps");
        public static readonly SettingId LightThemeSystem = new SettingId("com.microsoft.windows.lightTheme", "system");

        public string Solution { get; }
        public string Setting { get; }

        public SettingId(string solutionId, string settingId)
        {
            this.Solution = solutionId;
            this.Setting = settingId;
        }

        public override string ToString()
        {
            return $"{this.Solution}/{this.Setting}";
        }
    }
}
