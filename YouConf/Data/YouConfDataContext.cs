﻿using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using YouConf.Data.Entities;

namespace YouConf.Data
{
    public class YouConfDataContext : IYouConfDataContext
    {

        public YouConfDataContext()
        {

            var tableClient = GetTableClient();
            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("Conferences");
            table.CreateIfNotExists();
        }

        private CloudTableClient GetTableClient()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
               CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            return storageAccount.CreateCloudTableClient();
        }

        private CloudTable GetTable(string tableName)
        {
            var tableClient = GetTableClient();
            return tableClient.GetTableReference(tableName);
        }

        public IEnumerable<Conference> GetAllConferences()
        {

            //TODO: Yes I know that this will result in an unbounded select, however, once we start getting
            //a decent number of conferences we'll change how this works so it does a filter of some sort (TBD)
            var table = GetTable("Conferences");
            TableQuery<AzureTableEntity> query = new TableQuery<AzureTableEntity>();
            var conferences = table.ExecuteQuery(query);
            return conferences.Select(x =>  JsonConvert.DeserializeObject<Conference>(x.Entity));
        }

        public Conference GetConference(string hashTag)
        {
            //TODO: Yes I know that this will result in an unbounded select, however, once we start getting
            //a decent number of conferences we'll change how this works so it does a filter of some sort (TBD)
            var table = GetTable("Conferences");
            TableQuery<AzureTableEntity> query = new TableQuery<AzureTableEntity>();
            TableOperation retrieveOperation = TableOperation.Retrieve<AzureTableEntity>("Conferences", hashTag);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Conference>(((AzureTableEntity)(retrievedResult.Result)).Entity);
        }

        public void DeleteConference(string hashTag)
        {
            var table = GetTable("Conferences");
            TableQuery<AzureTableEntity> query = new TableQuery<AzureTableEntity>();
            TableOperation retrieveOperation = TableOperation.Retrieve<AzureTableEntity>("Conferences", hashTag);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                TableOperation deleteOperation = TableOperation.Delete((AzureTableEntity)retrievedResult.Result);
                // Execute the operation.
                table.Execute(deleteOperation);
            }
        }

        /// <summary>
        /// Inserts or updates a conference
        /// </summary>
        /// <param name="hashTag">The hashTag of the existing conference (for updates) or the hashTag of the new conference (for inserts)</param>
        /// <param name="conference">The conference itself</param>
        public void UpsertConference(string hashTag, Conference conference)
        {
            //Wrap the conference in our custom AzureTableEntity
            var table = GetTable("Conferences");

            //We're using the HashTag as the RowKey, so if it gets changed we have to remove the existing record and insert a new one
            //Yes I know that if the code fails after the deletion we could be left with no conference.... Maybe look at doing this in a batch operation instead?
            //Once I move this over to SQL for part 3 we can wrap it in a transaction
            if (hashTag != conference.HashTag)
            {
                DeleteConference(hashTag);
            }

            var entity = new AzureTableEntity()
            {
                PartitionKey = "Conferences",
                RowKey = conference.HashTag,
                Entity = JsonConvert.SerializeObject(conference)
            };

            TableOperation upsertOperation = TableOperation.InsertOrReplace(entity);

            // Insert or update the conference
            table.Execute(upsertOperation);
        }
    }
}