using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ChainedFunctions
{
    public class UploadLog
    {
        [FunctionName("WriteLog")]  
        [StorageAccount("DefaultEndpointsProtocol=https;AccountName=mlmdurablchainedstr;AccountKey=zddBBmtHQfWeGZZ6stm71O6YozG+LTQ3H8Cx//dSmNAFcqJ3mt7ZSWr1nnpytw9S4rW+F2di9lfZ+ASt4CfPKA==;EndpointSuffix=core.windows.net")]      
        public async Task Run(
            [BlobTrigger("heartbeat/{name}")] Stream uploadedBlob,
            [Table("heartbeats")] IAsyncCollector<HeartbeatLogEntity> entities,                          
            string name, ILogger log)
        {
            log.LogInformation($"New heartbeat blob uploaded:{name}");

            var entity = new HeartbeatLogEntity
            {
                PartitionKey = Guid.NewGuid().ToString().Substring(0,1),
                RowKey = Guid.NewGuid().ToString(),
                BlobName = name
            };
            await entities.AddAsync(entity);
            
            log.LogInformation("Recorded heartbeat in Table Storage");
        }
    }
}
