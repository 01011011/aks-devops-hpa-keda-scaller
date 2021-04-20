using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace storage_table_queue
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("QUEUE_CONNECTION");

            QueueClient queue = new QueueClient(connectionString, "jobqueue");

            string value = await RetrieveNextMessageAsync(queue);
            Console.WriteLine($"Received: {value}");
        }

        static async Task<string> RetrieveNextMessageAsync(QueueClient theQueue)
        {
            if (await theQueue.ExistsAsync())
            {
                QueueProperties properties = await theQueue.GetPropertiesAsync();

                if (properties.ApproximateMessagesCount > 0)
                {
                    QueueMessage[] retrievedMessage = await theQueue.ReceiveMessagesAsync(1);
                    string theMessage = retrievedMessage[0].MessageText;
                    await theQueue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                    return theMessage;
                }

                return null;
            }

            return null;
        }

    }
}
