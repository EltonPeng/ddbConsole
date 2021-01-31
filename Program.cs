using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace ddbConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Task t = MainAsync(args);
            t.Wait();

            Console.WriteLine("Ddb is done.");
        }

        public static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Start to connect ddb in container!");
            var clientConfig = new AmazonDynamoDBConfig();
            clientConfig.ServiceURL = "http://dynamodb-local:8000";
            var client = new AmazonDynamoDBClient(clientConfig);

            try 
            {
                var tableName = "HistoryRecord";
                var hashKey = "UserId";
                // Low Level API: client
                Console.WriteLine("-------Low Level API--------");
                Console.WriteLine("Verify table => " + tableName);
                var tableResponse = await client.ListTablesAsync();
                if (!tableResponse.TableNames.Contains(tableName))
                {
                    Console.WriteLine("Table not found, creating table => " + tableName);
                    await client.CreateTableAsync(new CreateTableRequest
                    {
                        TableName = tableName,
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 3,
                            WriteCapacityUnits = 1
                        },
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = hashKey,
                                KeyType = KeyType.HASH
                            }
                        },
                        AttributeDefinitions = new List<AttributeDefinition>
                        {
                            new AttributeDefinition { AttributeName = hashKey, AttributeType=ScalarAttributeType.S }
                        }
                    });
                    
                    bool isTableAvailable = false;
                    while (!isTableAvailable) {
                        Console.WriteLine("Waiting for table to be active...");
                        Thread.Sleep(2000);
                        var tableStatus = await client.DescribeTableAsync(tableName);
                        isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
                    }
                }
                Console.WriteLine("low-lavel. Save data to table");
                var itemDict = new Dictionary<String, AttributeValue>();
                itemDict.Add("UserId", new AttributeValue() { S = "1" });
                itemDict.Add("Birthday", new AttributeValue() { S = "1997-7-1" });


                var request =  new PutItemRequest() { TableName=tableName, Item = itemDict };
                var result = await client.PutItemAsync(request);
                Console.WriteLine("low-lavel. Save result: " + result.HttpStatusCode);


                // Document Model: Table
                Console.WriteLine("-------Document Model--------");
                var table = Table.LoadTable(client, tableName);
                var item = new Document();
                item["UserId"] = "2";
                //item["Birthday"] = ??? 
                await table.PutItemAsync(item);
                var getConfig = new GetItemOperationConfig()
                {
                    AttributesToGet = new List<string>() { "UserId", "Birthday" },
                    ConsistentRead = true
                };
                var doc = await table.GetItemAsync("1", getConfig);
                Console.WriteLine("table. get 1st item birthday: " + doc["Birthday"]);

                // Persistence Model: Context
                Console.WriteLine("-------Persistence Model--------");
                var context = new DynamoDBContext(client);

                var newRecord = new HistoryRecord
                {
                    UserId = "3",
                    Line = new RecordLine()
                    {
                        Token = "awesomeToken",
                        Index = 0,
                        Finished = true,
                        StuffIds = new List<int> { 0, 1 }
                    }
                };

                await context.SaveAsync<HistoryRecord>(newRecord);

                Console.WriteLine("context. getting 3nd HistoryRecord object");
                List<ScanCondition> conditions = new List<ScanCondition>();
                conditions.Add(new ScanCondition("UserId", ScanOperator.Equal, newRecord.UserId));
                var allDocs = await context.ScanAsync<HistoryRecord>(conditions).GetRemainingAsync();
                var savedState = allDocs.FirstOrDefault();

                // ToDocument
                var docFromP = context.ToDocument<HistoryRecord>(savedState);

                if (JsonConvert.SerializeObject(savedState) == JsonConvert.SerializeObject(newRecord))
                    Console.WriteLine("Retrieved is: " + savedState.Line.Token);
                else
                    Console.WriteLine("Oops, saved item is differnt");

            } 
            catch (Exception e) 
            {
                Console.WriteLine("!!!!!! ");
                Console.WriteLine(e.Message);
            }
       }
    }
}
