﻿namespace Morphic.Settings.SettingsHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class Setting
    {
        public Setting()
        {
        }

        private EventHandler<SettingEventArgs>? changedHandler;
        public event EventHandler<SettingEventArgs>? Changed
        {
            add
            {
                if (this.changedHandler == null)
                {
                    this.SettingGroup.SettingsHandler.AddSettingListener(this);
                }

                this.changedHandler += value;
            }
            remove
            {
                this.changedHandler -= value;
                if (this.changedHandler == null)
                {
                    this.SettingGroup.SettingsHandler.RemoveSettingListener(this);
                }
            }
        }

        public SettingGroup SettingGroup { get; private set; } = null!;

        public string Id { get; private set; } = null!;

        /// <summary>Name of the setting, used by the setting handler.</summary>
        [JsonProperty("name")]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("dataType")]
        public SettingType DataType { get; private set; }

        /// <summary>Don't copy this setting to/from another computer.</summary>
        [JsonProperty("local")]
        public bool Local { get; private set; }

        [JsonProperty("range")]
        public SettingRange? Range { get; private set; }

        [JsonProperty("changes")]
        public SettingChanges? SettingChanges { get; private set; }

        public object? CurrentValue { get; private set; }

        /// <summary>Gets the value of this setting.</summary>
        public async Task<object?> GetValue()
        {
            this.CurrentValue = await this.SettingGroup.SettingsHandler.Get(this);
            return this.CurrentValue;
        }

        /// <summary>Gets the value of this setting.</summary>
        public async Task<T> GetValue<T>(T defaultValue = default)
        {
            object? value = await this.GetValue();
            T result;
            if (value is T v)
            {
                result = v;
            }
            else
            {
                result = defaultValue;
            }

            this.CurrentValue = result;
            return result;
        }

        /// <summary>Sets the value of this setting.</summary>
        public Task<bool> SetValue(object? newValue)
        {
            this.CurrentValue = newValue;
            return this.SettingGroup.SettingsHandler.Set(this, newValue);
        }

        public async Task<bool> CheckForChange()
        {
            object? oldValue = this.CurrentValue;
            object? newValue = await this.GetValue();
            bool changed = oldValue != newValue;
            if (changed)
            {
                this.OnSettingChanged(newValue);
            }
            return changed;
        }

        private void OnSettingChanged(object? newValue)
        {
            this.changedHandler?.Invoke(this, new SettingEventArgs(this, newValue));
        }

        public async Task<bool> Increment(int direction)
        {
            if (this.Range != null)
            {
                int current = await this.GetValue<int>();
                current += Math.Sign(direction) * this.Range.IncrementValue;
                if (current > await this.Range.GetMin() && current < await this.Range.GetMax())
                {
                    return await this.SetValue(current);
                }
            }
            return false;
        }

        public virtual void Deserialized(SettingGroup settingGroup, string settingId)
        {
            this.SettingGroup = settingGroup;
            this.Id = settingId;
            if (string.IsNullOrEmpty(this.Name))
            {
                this.Name = this.Id;
            }

            this.Range?.Deserialized(this);
        }

        public static explicit operator Setting(string compact)
        {
            string[] parts = compact.Split(':', 2);
            Setting setting = new Setting()
            {
                Name = parts[0],
            };
            if (parts.Length > 1)
            {
                if (Enum.TryParse(parts[1], true, out SettingType t))
                {
                    setting.DataType = t;
                }
            }

            return setting;
        }
    }

    public enum SettingType
    {
        Unknown = 0,
        Int = 1,
        Real = 2,
        Bool = 3,
        String = 4
    }

    public class SettingEventArgs : EventArgs
    {
        public SettingEventArgs(Setting setting, object? newValue)
        {
            this.Setting = setting;
            this.NewValue = newValue;
        }

        public Setting Setting { get; }
        public object? NewValue { get; }


    }

    public class SettingChanges
    {
        [JsonProperty("path")]
        public string Path { get; private set; } = string.Empty;
        [JsonProperty("monitorType")]
        public string MonitorType { get; private set; } = string.Empty;

        public static explicit operator SettingChanges(string path)
        {
            string[] parts = path.Split(':', 2);
            return new SettingChanges()
            {
                MonitorType = parts[0],
                Path = parts[1]
            };
        }


    }

    [JsonObject(MemberSerialization.OptIn)]
    public class SettingRange
    {
        private int? minValue;
        private int? maxValue;

        [JsonProperty("min", Required = Required.Always)]
        private Limit Min { get; set; } = null!;

        [JsonProperty("max", Required = Required.Always)]
        private Limit Max { get; set; } = null!;

        [JsonProperty("inc")]
        public int IncrementValue { get; private set; } = 1;

        [JsonProperty("live")]
        public bool Live { get; private set; }

        public async Task<int> GetMin(int defaultResult = 0)
        {
            if (this.Live || !this.minValue.HasValue)
            {
                this.minValue = await this.Min.Get(defaultResult);
            }

            return this.minValue.Value;
        }

        public async Task<int> GetMax(int defaultResult = 0)
        {
            if (this.Live || !this.maxValue.HasValue)
            {
                this.maxValue = await this.Max.Get(defaultResult);
            }

            return this.maxValue.Value;
        }

        public Setting Setting { get; private set; } = null!;

        public void Deserialized(Setting setting)
        {
            this.Setting = setting;
            this.Min.Deserialized(this.Setting);
            this.Max.Deserialized(this.Setting);
        }

        private class Limit
        {
            private Setting? setting;
            private Setting? parentSetting;
            private string? settingId;
            private string? expression;
            private int? value;
            private int increment;
            private Limit? defaultValue;

            // "settingId [ (+|-) increment] [ ? default]"
            private readonly Regex parseLimit = new Regex(@"^(?<setting>\S+)(\s+(?<sign>[-+])\s*(?<increment>\S+)?(\s*\?\s*(?<default>\S+)))?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);


            protected Limit()
            {
            }

            public async Task<int> Get(int defaultResult = 0)
            {
                if (this.value.HasValue)
                {
                    return this.value.Value;
                }

                if (this.settingId != null && this.setting == null)
                {
                    this.setting = this.parentSetting?.SettingGroup.Solution.ResolveSettingId(this.settingId);
                }

                if (this.setting != null)
                {
                    int num = await this.setting.GetValue(this.defaultValue?.value ?? int.MinValue);
                    if (num != int.MinValue)
                    {
                        return num;
                    }
                }

                if (this.defaultValue != null)
                {
                    return await this.defaultValue.Get();
                }

                return defaultResult;
            }

            private static Limit FromString(string expr)
            {
                if (int.TryParse(expr, out int number))
                {
                    return new Limit() { value = number };
                }

                return new Limit() { expression = expr };
            }

            public static implicit operator Limit(int number) => new Limit() { value = number };
            public static implicit operator Limit(long number) => new Limit() { value = (int)number };
            public static implicit operator Limit(string expr) => FromString(expr);

            public void Deserialized(Setting parent)
            {
                this.parentSetting = parent;
                if (this.expression != null)
                {
                    // Parse the expression.
                    Match match = this.parseLimit.Match(this.expression.Trim());
                    this.settingId = match.Groups["setting"].Value;
                    if (match.Groups["increment"].Success)
                    {
                        this.increment = ((IConvertible)match.Groups["increment"].Value).ToInt32(null);
                        if (match.Groups["sign"].Value == "-")
                        {
                            this.increment = -this.increment;
                        }

                        if (match.Groups["default"].Success)
                        {
                            this.defaultValue = FromString(match.Groups["default"].Value);
                        }
                    }
                }
            }
        }

    }

    public static class SettingExtensions
    {
        public static Dictionary<string, Setting> ToDict(this IEnumerable<Setting> settings)
        {
            return settings.ToDictionary(setting => setting.Name, setting => setting);
        }

        public static IServiceProvider GetServiceProvider(this Setting setting)
        {
            return setting.SettingGroup.Solution.Solutions.ServiceProvider;
        }
    }
}
