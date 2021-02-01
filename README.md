# ddbConsole
DynamoDB in .NET Core 3.1 console application

## CMDs
dotnet new console -n ddbConsole

dotnet add package AWSSDK.DynamoDBv2

dotnet add package Newtonsoft.Json

vi Dockerfile

docker build -t ddb-console .

vi docker-compose.yaml

docker-compose up
