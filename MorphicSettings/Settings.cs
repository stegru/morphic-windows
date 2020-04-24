﻿// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using MorphicCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MorphicSettings
{
    public class Settings
    {

        public Settings(IServiceProvider provider, ILogger<Settings> logger)
        {
            this.logger = logger;
            this.provider = provider;
        }

        private readonly ILogger<Settings> logger;
        private readonly IServiceProvider provider;

        public bool Apply(Preferences.Key key, object? value)
        {
            logger.LogInformation("Apply {0}", key);
            if (SettingsHandler.Handler(provider, key) is SettingsHandler handler)
            {
                try
                {
                    if (handler.Apply(value))
                    {
                        return true;
                    }
                    logger.LogError("Failed to set {0}", key);
                    return false;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to set {0}", key);
                    return false;
                }
            }
            else
            {
                logger.LogInformation("No handler for {0}", key);
                return false;
            }
        }

        public static class Keys
        {
            public static Preferences.Key WindowsDisplayZoom = new Preferences.Key("com.microsoft.windows.display", "zoom");
        }
    }

    public static class SettingsServiceProvider
    {
        public static void AddMorphicSettingsHandlers(this IServiceCollection services, Action<SettingsHandlerBuilder> callback)
        {
            var builder = new SettingsHandlerBuilder(services);
            builder.AddHandler(typeof(DisplayZoomHandler), Settings.Keys.WindowsDisplayZoom);
            callback(builder);
        }
    }
}