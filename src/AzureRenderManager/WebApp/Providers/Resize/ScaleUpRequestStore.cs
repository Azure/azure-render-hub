// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebApp.Providers.Resize
{
    public class ScaleUpRequestStore : IScaleUpRequestStore
    {
        private const string TableName = "ScaleUpRequestStore";
        private readonly CloudTable _table;
        private readonly ILogger<IScaleUpRequestStore> _logger;

        public ScaleUpRequestStore(CloudTableClient tableClient, ILogger<IScaleUpRequestStore> logger)
        {
            _table = tableClient.GetTableReference(TableName);
            _logger = logger;
        }

        public async Task Add(string envName, string poolName, int requested)
        {
            async Task AddRequest()
            {
                var scaleUpEntry = await Get(envName, poolName, CancellationToken.None);
                if (scaleUpEntry == null)
                {
                    // no entry exists so add it
                    scaleUpEntry =
                        new ScaleUpRequestEntity(envName, poolName)
                        {
                            TargetNodes = requested,
                        };

                    await InsertOrMergeEntry(scaleUpEntry);
                }
                else
                {
                    bool changed = false;
                    if (requested > scaleUpEntry.TargetNodes)
                    {
                        changed = true;
                        scaleUpEntry.TargetNodes = requested;
                    }

                    // if limits were lower then the request is ignored
                    if (changed)
                    {
                        await InsertOrMergeEntry(scaleUpEntry);
                    }
                }
            }

            // we will attempt up to 5 times
            // this should be plenty as there is a large delay on the processor side
            StorageException lastEx = null;
            for (int i = 0; i < 5; ++i)
            {
                try
                {
                    await AddRequest();
                    return;
                }
                catch (StorageException ex)
                    when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    // etag mismatch, try again
                    lastEx = ex;
                }
            }

            // give up:
            _logger.LogError(lastEx, "Unable to insert scale request");
            throw lastEx;
        }

        public async Task<ScaleUpRequestEntity> Get(string envName, string poolName, CancellationToken ct)
        {
            var operation = TableOperation.Retrieve<ScaleUpRequestEntity>(envName, poolName);
            try
            {
                var result = await _table.ExecuteAsync(operation, requestOptions: null, operationContext: null, cancellationToken: ct);
                return result.Result as ScaleUpRequestEntity;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // table is gone
                return null;
            }
        }

        public async Task<IReadOnlyList<ScaleUpRequestEntity>> List(CancellationToken ct)
        {
            var entities = new List<ScaleUpRequestEntity>();

            var query = new TableQuery<ScaleUpRequestEntity>();

            // get all of the entries in the table ... dangerous? shouldn't be too bad as 
            // entries will be deleted when they have been processed.

            TableContinuationToken token = null;
            try
            {
                do
                {
                    var queryResult =
                        await _table.ExecuteQuerySegmentedAsync(
                            query,
                            token,
                            requestOptions: null,
                            operationContext: null,
                            cancellationToken: ct);

                    token = queryResult.ContinuationToken;
                    entities.AddRange(queryResult.Results);

                } while (token != null);

                return entities;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // table doesn't exist, return empty list
                return Array.Empty<ScaleUpRequestEntity>();
            }
        }

        public async Task Delete(ScaleUpRequestEntity entry)
        {
            Console.WriteLine("Deleting entry '{0}:{1}' from storage", entry.EnvironmentName, entry.PoolName);

            try
            {
                var operation = TableOperation.Delete(entry);

                await _table.ExecuteAsync(operation);
                Console.WriteLine("Entry '{0}:{1}' deleted", entry.EnvironmentName, entry.PoolName);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // whole table is gone
            }
            catch (Exception ex)
            {
                // TODO: correctly handle this. This may fail due to eTag. 
                // if so read it again and delete.
                Console.WriteLine("Failed to delete storage entry: {0}", ex);
            }
        }

        private async Task InsertOrMergeEntry(ScaleUpRequestEntity entry)
        {
            // TODO: catch and retry if it fails as another request could have already added one
            // for the same environment and pool
            await _table.CreateIfNotExistsAsync();

            var operation = TableOperation.InsertOrMerge(entry);
            await _table.ExecuteAsync(operation);
        }
    }
}
