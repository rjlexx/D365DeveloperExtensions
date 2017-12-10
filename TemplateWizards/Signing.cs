﻿using CrmDeveloperExtensions2.Core;
using CrmDeveloperExtensions2.Core.Models;
using CrmDeveloperExtensions2.Core.UserOptions;
using EnvDTE;
using NLog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using StatusBar = CrmDeveloperExtensions2.Core.StatusBar;

namespace TemplateWizards
{
    public static class Signing
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [DllImport("mscoree.dll")]
        internal static extern int StrongNameFreeBuffer(IntPtr pbMemory);
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int StrongNameKeyGen(IntPtr wszKeyContainer, uint dwFlags, out IntPtr keyBlob, out uint keyBlobSize);
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        internal static extern int StrongNameErrorInfo();

        public static void GenerateKey(Project project, string destDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(destDirectory))
                    return;

                StatusBar.SetStatusBarValue(Resources.Resource.GeneratingKeyStatusBarMessage);

                var defaultKeyName = CreateKeyName(destDirectory);

                string keyFilePath = Path.Combine(destDirectory, defaultKeyName);

                IntPtr buffer = IntPtr.Zero;

                WriteKeydata(buffer, keyFilePath);

                project.Properties.Item("SignAssembly").Value = "true";
                project.Properties.Item("AssemblyOriginatorKeyFile").Value = defaultKeyName;
                project.ProjectItems.AddFromFile(keyFilePath);
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(Logger, Resources.Resource.GeneratingKeyFailureMessage, ex);
                MessageBox.Show(Resources.Resource.GeneratingKeyFailureMessage);
            }
            finally
            {
                StatusBar.ClearStatusBarValue();
            }
        }

        private static string CreateKeyName(string destDirectory)
        {
            string defaultKeyName = UserOptionsHelper.GetOption<string>(UserOptionProperties.DefaultKeyFileName);
            if (!FileSystem.IsValidFilename(defaultKeyName, destDirectory))
                defaultKeyName = CrmDeveloperExtensions2.Core.Resources.Resource.DefaultKeyName;

            if (!defaultKeyName.EndsWith(".snk", StringComparison.CurrentCultureIgnoreCase))
                defaultKeyName = $"{defaultKeyName}.snk";

            return defaultKeyName;
        }

        private static void WriteKeydata(IntPtr buffer, string keyFilePath)
        {
            try
            {
                if (0 != StrongNameKeyGen(IntPtr.Zero, 0, out buffer, out var buffSize))
                    Marshal.ThrowExceptionForHR(StrongNameErrorInfo());
                if (buffer == IntPtr.Zero)
                    throw new InvalidOperationException();

                var keyBuffer = new byte[buffSize];
                Marshal.Copy(buffer, keyBuffer, 0, (int)buffSize);
                File.WriteAllBytes(keyFilePath, keyBuffer);
            }
            finally
            {
                StrongNameFreeBuffer(buffer);
            }
        }
    }
}