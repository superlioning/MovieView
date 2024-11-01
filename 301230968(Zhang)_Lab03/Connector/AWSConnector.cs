using Amazon;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Configuration;

namespace _301230968_Zhang__Lab03.Connector
{
    public class AWSConnector
    {
        public IAmazonS3 S3Client { get; }
        public IAmazonDynamoDB DynamoClient { get; }
        public DynamoDBContext Context { get; }

        public AWSConnector(IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            var credentials = new BasicAWSCredentials(awsOptions["AccessKeyId"], awsOptions["SecretAccessKey"]);
            var region = RegionEndpoint.CACentral1;

            S3Client = new AmazonS3Client(credentials, region);
            DynamoClient = new AmazonDynamoDBClient(credentials, region);
            Context = new DynamoDBContext(DynamoClient);
        }

        public Table LoadContentTable(string tableName)
        {
            return Table.LoadTable(DynamoClient, tableName);
        }
    }
}
