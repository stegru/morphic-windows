// BarManager.cs: Loads and shows bar.
//
// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt


namespace Morphic.Client.Bar
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using Config;
    using Core;
    using Core.Community;
    using Data;
    using Dialogs;
    using Microsoft.Extensions.Logging;
    using Service;
    using UI;
    using MessageBox = System.Windows.Forms.MessageBox;
    using SystemJson = System.Text.Json;

    /// <summary>
    /// Looks after the bar.
    /// </summary>
    public class BarManager : INotifyPropertyChanged
    {
        private PrimaryBarWindow? barWindow;
        private ILogger Logger => App.Current.Logger;

        public event EventHandler<BarEventArgs>? BarLoaded;
        public event EventHandler<BarEventArgs>? BarUnloaded;

        private bool firstBar = true;

        public bool BarVisible => this.barWindow?.Visibility == Visibility.Visible;

        public BarManager()
        {
        }

        /// <summary>
        /// Show a bar that's already loaded.
        /// </summary>
        public void ShowBar()
        {
            if (this.barWindow != null)
            {
                this.barWindow.Visibility = Visibility.Visible;
                this.barWindow.Focus();
            }
        }

        public void HideBar()
        {
            this.barWindow?.Hide();
            this.barWindow?.OtherWindow?.Hide();
        }

        /// <summary>
        /// Closes the bar.
        /// </summary>
        public void CloseBar()
        {
            if (this.barWindow != null)
            {
                this.OnBarUnloaded(this.barWindow);
                BarData bar = this.barWindow.Bar;
                this.barWindow.IsClosing = true;
                this.barWindow.Close();
                this.barWindow = null;
                bar.Dispose();
            }
        }

        public BarWindow CreateBarWindow(BarData bar)
        {
            this.barWindow = new PrimaryBarWindow(bar);
            this.barWindow.BarLoaded += this.OnBarLoaded;
            this.barWindow.IsVisibleChanged += this.BarWindowOnIsVisibleChanged;

            if (AppOptions.Current.AutoShow || !this.firstBar)
            {
                this.barWindow.Show();
            }

            this.firstBar = false;
            return this.barWindow;
        }

        private void BarWindowOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.BarVisible));
        }

        /// <summary>
        /// Called when a bar has loaded.
        /// </summary>
        protected virtual void OnBarLoaded(object sender, EventArgs? args = null)
        {
            if (sender is PrimaryBarWindow window)
            {
                this.BarLoaded?.Invoke(this, new BarEventArgs(window));
            }
        }

        /// <summary>
        /// Called when a bar has closed.
        /// </summary>
        protected virtual void OnBarUnloaded(object sender, EventArgs? args = null)
        {
            if (sender is PrimaryBarWindow window)
            {
                this.BarUnloaded?.Invoke(this, new BarEventArgs(window));
            }
        }

        #region DataLoading
        private void OnBarOnReloadRequired(object? sender, EventArgs args)
        {
            if (sender is BarData bar)
            {
                string source = bar.Source;

                this.CloseBar();
                this.LoadFromBarJson(source);
            }
        }

        /// <summary>
        /// Loads and shows a bar.
        /// </summary>
        /// <param name="path">JSON file containing the bar data.</param>
        /// <param name="content">The file content (if it's already loaded).</param>
        /// <param name="serviceProvider"></param>
        public BarData? LoadFromBarJson(string path, string? content = null, IServiceProvider? serviceProvider = null)
        {
            if (this.firstBar && AppOptions.Current.Launch.BarFile != null)
            {
                path = AppOptions.Current.Launch.BarFile;
            }

            BarData? bar = null;
            try
            {
                bar = BarData.Load(serviceProvider ?? App.Current.ServiceProvider, path, content);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                this.Logger.LogError(e, "Problem loading the bar.");
            }

            if (this.barWindow != null)
            {
                this.CloseBar();
            }

            if (bar != null)
            {
                this.CreateBarWindow(bar);
                bar.ReloadRequired += this.OnBarOnReloadRequired;
            }

            return bar;
        }

        /// <summary>
        /// Loads the bar for the given session. If the user is a member of several, either the last one is used,
        /// or a selection dialog is presented.
        /// </summary>
        /// <param name="session">The current session.</param>
        /// <param name="showCommunityId">Force this community to show.</param>
        public async void LoadSessionBar(CommunitySession session, string? showCommunityId = null)
        {
            if (this.firstBar && AppOptions.Current.Launch.BarFile != null)
            {
                this.LoadFromBarJson(AppOptions.Current.Launch.BarFile);
                return;
            }

            this.Logger.LogInformation($"Loading a bar ({session.Communities.Length} communities)");

            UserBar? bar;

            string[] lastCommunities = AppOptions.Current.Communities.ToArray();
            string? lastCommunityId = showCommunityId ?? AppOptions.Current.LastCommunity;

            if (string.IsNullOrWhiteSpace(lastCommunityId))
            {
                lastCommunityId = null;
            }

            UserCommunity? community = null;
            UserBar? userBar = null;

            if (session.Communities.Length == 0)
            {
                MessageBox.Show("You are not part of a Morphic community yet.", "Morphic");
            }
            else if (session.Communities.Length == 1)
            {
                community = session.Communities.First();
            }
            else
            {
                // The user is a member of multiple communities.

                // See if any membership has changed
                bool changed = showCommunityId != null && session.Communities.Length != lastCommunities.Length
                    || !session.Communities.Select(c => c.Id).OrderBy(id => id)
                        .SequenceEqual(lastCommunities.OrderBy(id => id));

                if (!changed && lastCommunityId != null)
                {
                    community = session.Communities.FirstOrDefault(c => c.Id == lastCommunityId);
                }

                if (community == null)
                {
                    this.Logger.LogInformation("Showing community picker");

                    // Load the bars while the picker is shown
                    Dictionary<string, Task<UserBar>> bars =
                        session.Communities.ToDictionary(c => c.Id, c => session.GetBar(c.Id));

                    // Show the picker
                    CommunityPickerWindow picker = new CommunityPickerWindow(session.Communities);
                    bool gotCommunity = picker.ShowDialog() == true;
                    community = gotCommunity ? picker.SelectedCommunity : null;

                    if (community != null)
                    {
                        userBar = await bars[community.Id];
                    }
                }
            }

            if (community != null)
            {
                userBar ??= await session.GetBar(community.Id);

                this.Logger.LogInformation($"Showing bar for community {community.Id} {community.Name}");
                string barJson = this.GetUserBarJson(userBar);
                BarData? barData = this.LoadFromBarJson(userBar.Id, barJson);
                if (barData != null)
                {
                    barData.CommunityId = community.Id;
                }
            }

            AppOptions.Current.Communities = session.Communities.Select(c => c.Id).ToArray();
            AppOptions.Current.LastCommunity = community?.Id;
        }

        /// <summary>
        /// Gets the json for a <see cref="UserBar"/>, so it can be loaded with a better deserialiser.
        /// </summary>
        /// <param name="userBar">Bar data object from Morphic.Core</param>
        private string GetUserBarJson(UserBar userBar)
        {
            // Serialise the bar data so it can be loaded with a better deserialiser.
            SystemJson.JsonSerializerOptions serializerOptions = new SystemJson.JsonSerializerOptions();
            serializerOptions.Converters.Add(new JsonElementInferredTypeConverter());
            serializerOptions.Converters.Add(
                new SystemJson.Serialization.JsonStringEnumConverter(SystemJson.JsonNamingPolicy.CamelCase));
            string barJson = SystemJson.JsonSerializer.Serialize(userBar, serializerOptions);

            // Dump to a file, for debugging.
            string barFile = AppPaths.GetConfigFile("last-bar.json5");
            File.WriteAllText(barFile, barJson);

            return barJson;
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }

    public class BarEventArgs : EventArgs
    {
        public BarEventArgs(PrimaryBarWindow window)
        {
            this.Window = window;
            this.Bar = this.Window.Bar;
        }

        public BarData Bar { get; private set; }
        public PrimaryBarWindow Window { get; private set; }

    }
}
