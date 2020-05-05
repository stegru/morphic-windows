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
using System.Threading.Tasks;
using System.Security;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Morphic.Windows.Native;

namespace MorphicSettings
{
    /// <summary>
    /// A settings handler for ini files
    /// </summary>
    class IniSettingsHandler : SettingHandler
    {

        /// <summary>
        /// The handler descrition indicating what file/section/key to read/write
        /// </summary>
        public IniSettingHandlerDescription Description { get; private set; }

        /// <summary>
        /// Create a new ini handler from the given handler description
        /// </summary>
        /// <param name="description"></param>
        /// <param name="logger"></param>
        public IniSettingsHandler(IniSettingHandlerDescription description, ILogger<IniSettingsHandler> logger)
        {
            Description = description;
            this.logger = logger;
            var path = ExpandedPath(description.Filename);
            iniFile = new IniFileReaderWriter(path);
        }

        /// <summary>
        /// Expand certain whitelisted environmental variables in a path template
        /// </summary>
        /// <param name="templatePath"></param>
        /// <returns></returns>
        private static string ExpandedPath(string templatePath)
        {
            var allowedVariables = new string[]
            {
                "APPDATA"
            };
            var path = templatePath;
            foreach (var varname in allowedVariables)
            {
                path = path.Replace($"$({varname})", Environment.GetEnvironmentVariable(varname), StringComparison.OrdinalIgnoreCase);
            }
            return path;
        }

        /// <summary>
        /// The logger to user
        /// </summary>
        private readonly ILogger<IniSettingsHandler> logger;

        /// <summary>
        /// The ini file reader/writer
        /// </summary>
        private readonly IniFileReaderWriter iniFile;

        /// <summary>
        /// Write the value to the section+key
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Task<bool> Apply(object? value)
        {
            // FIXME: ToString() probably isn't correct for all types
            if (value?.ToString() is string stringValue)
            {
                try
                {
                    logger.LogDebug("Writing {0}:{1}.{2}", Description.Filename, Description.Section, Description.Key);
                    iniFile.WriteValue(stringValue, Description.Key, Description.Section);
                    return Task.FromResult(true);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to set ini value");
                }
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Read the value from the section+key
        /// </summary>
        /// <returns></returns>
        public override Task<CaptureResult> Capture()
        {
            var result = new CaptureResult();
            try
            {
                // FIXME: need to parse correct type from string
                result.Value = iniFile.ReadValue(Description.Key, Description.Section);
                result.Success = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to capture ini value");
            }
            return Task.FromResult(result);
        }
    }
}