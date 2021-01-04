﻿/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxPackageExplorer;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Threading;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is retrieved by an network/ HTTP commands and
    /// buffered locally.
    /// </summary>
    public class PackageContainerNetworkHttpFile : PackageContainerBuffered
    {
        /// <summary>
        /// Uri of an AASX retrieved by HTTP
        /// </summary>
        public Uri SourceUri;

        //
        // Constructors
        //

        public PackageContainerNetworkHttpFile()
        {
            Init();
        }

        public PackageContainerNetworkHttpFile(
            PackageCentral packageCentral,
            string sourceFn)
            : base (packageCentral)
        {
            Init();
            SetNewSourceFn(sourceFn);
        }

        // nice discussion on how to name factory-like methods

        public static async Task<PackageContainerNetworkHttpFile> CreateAsync(
            PackageCentral packageCentral,
            string sourceFn, bool loadResident = false,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerNetworkHttpFile(packageCentral, sourceFn);
            res.LoadResident = loadResident;
            if (res.LoadResident)
                await res.InternalLoadFromSourceAsync(runtimeOptions);
            return res;
        }

        //
        // Mechanics
        //

        public override string Filename { get { return SourceUri.ToString(); } }

        private void Init()
        {
            this.LoadFromSource = this.InternalLoadFromSourceSync;
            // this.SaveAsToSource = this.InternalSaveToSource;
        }

        private void SetNewSourceFn(string sourceUri)
        {
            SourceUri = new Uri(sourceUri);
            IsFormat = Format.AASX;
            IndirectLoadSave = true;
        }

        public override string ToString()
        {
            return "HTTP file: " + SourceUri;
        }

        private async Task DownloadFromSource(Uri sourceUri,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            // get response?
            using (var response = await client.GetAsync(requestPath,
                HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;
                var contentFn = response.Content.Headers.ContentDisposition?.FileName;

                // log
                runtimeOptions?.Log?.Info($".. response with header-content-len {contentLength} " +
                    $"and file-name {contentFn} ..");

                var contentStream = await response?.Content?.ReadAsStreamAsync();
                if (contentStream == null)
                    throw new PackageContainerException(
                    $"While getting data bytes from {SourceUri.ToString()} via HttpClient " +
                    $"no data-content was responded!");

                // create temp file and write to it
                var givenFn = SourceUri.ToString();
                if (contentFn != null)
                    givenFn = contentFn;
                TempFn = CreateNewTempFn(givenFn, IsFormat);
                runtimeOptions?.Log?.Info($".. downloading to temp-file {TempFn}");

                using (var file = new FileStream(TempFn, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // copy with progress
                    var bufferSize = 512*1024;
                    var buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                        default(CancellationToken)).ConfigureAwait(false)) != 0)
                    {
                        await file.WriteAsync(buffer, 0, bytesRead,
                            default(CancellationToken)).ConfigureAwait(false);
                                                                      
                        totalBytesRead += bytesRead;
                        runtimeOptions?.ProgressChanged?.Invoke(contentLength, totalBytesRead);
                    }

                    // assume bytes read to be total bytes
                    runtimeOptions?.ProgressChanged?.Invoke(totalBytesRead, totalBytesRead);

                    // log                
                    runtimeOptions?.Log?.Info($".. download done with {totalBytesRead} bytes read!");
                }

            }
        }

        private void InternalLoadFromSourceSync(PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var task = Task.Run(() => InternalLoadFromSourceAsync(runtimeOptions));
            task.Wait();
        }

        private async Task InternalLoadFromSourceAsync(
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // buffer to temp file
            try
            {
                await DownloadFromSource(SourceUri, runtimeOptions);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {SourceUri.ToString()} via HttpClient " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            // open
            try
            {
                Env = new AdminShellPackageEnv(TempFn, indirectLoadSave: false);
                runtimeOptions?.Log?.Info($".. successfully opened as AASX environment: {Env?.AasEnv?.ToString()}");
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While opening buffered aasx {TempFn} from source {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }
    }
}