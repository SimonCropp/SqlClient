﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using Microsoft.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Data.SqlClient.ManualTesting.Tests
{
    public class JsonTest
    {
        private readonly ITestOutputHelper _output;

        public JsonTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private static readonly string jsonDataString = "[{\"name\":\"Dave\",\"skills\":[\"Python\"]},{\"name\":\"Ron\",\"surname\":\"Peter\"}]";

        private void ValidateRowsAffected(int rowsAffected)
        {
            _output.WriteLine($"Rows affected: {rowsAffected}");
            Assert.Equal(1, rowsAffected);
        }

        private void ValidateRows(SqlDataReader reader)
        {
            while (reader.Read())
            {
                string jsonData = reader.GetString(0);
                _output.WriteLine(jsonData);
                Assert.Equal(jsonDataString, jsonData);
            }
        }

        private async Task ValidateRowsAsync(SqlDataReader reader)
        {
            while (await reader.ReadAsync())
            {
                string jsonData = reader.GetString(0);
                _output.WriteLine(jsonData);
                Assert.Equal(jsonDataString, jsonData);
            }
        }

        private void ValidateSchema(SqlDataReader reader)
        {
            System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> schema = reader.GetColumnSchema();
            foreach (DbColumn column in schema)
            {
                _output.WriteLine("Column Name is " + column.ColumnName);
                _output.WriteLine("Column DataType is " + column?.DataType.ToString());
                _output.WriteLine("Column DataTypeName is " + column.DataTypeName);
                Assert.Equal("json", column.DataTypeName);
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.IsJsonSupported))]
        public void TestJsonWrite()
        {       
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("Json_Test");
            string spName = DataTestUtility.GetUniqueNameForSqlServer("spJson_WriteTest");
           
            string tableCreate = "CREATE TABLE " + tableName + " (Data json)";
            string tableInsert = "INSERT INTO " + tableName + " VALUES (@jsonData)";
            string spCreate = "CREATE PROCEDURE " + spName + " (@jsonData json) AS " + tableInsert;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    //Create Table
                    command.CommandText = tableCreate;
                    command.ExecuteNonQuery();

                    //Create SP for writing json values
                    command.CommandText = spCreate;
                    command.ExecuteNonQuery();

                    command.CommandText = tableInsert;
                    var parameter = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                    command.Parameters.Add(parameter);

                    //Test 1
                    //Write json value using a parameterized query
                    parameter.Value = jsonDataString;
                    int rowsAffected = command.ExecuteNonQuery();
                    ValidateRowsAffected(rowsAffected);

                    //Test 2 
                    //Write a SqlString type as json
                    parameter.Value = new SqlString(jsonDataString);
                    int rowsAffected2 = command.ExecuteNonQuery();
                    ValidateRowsAffected(rowsAffected2);

                    //Test 3
                    //Write json value using SP
                    using (SqlCommand command2 = connection.CreateCommand())
                    {
                        command2.CommandText = spName;
                        command2.CommandType = CommandType.StoredProcedure;
                        var parameter2 = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                        parameter2.Value = jsonDataString;
                        command2.Parameters.Add(parameter2);
                        int rowsAffected3 = command2.ExecuteNonQuery();
                        ValidateRowsAffected(rowsAffected3);
                    }

                    DataTestUtility.DropTable(connection, tableName);
                }
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.IsJsonSupported))]
        public async Task TestJsonWriteAsync()
        {
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("Json_Test");
            string spName = DataTestUtility.GetUniqueNameForSqlServer("spJson_WriteTest");

            string tableCreate = "CREATE TABLE " + tableName + " (Data json)";
            string tableInsert = "INSERT INTO " + tableName + " VALUES (@jsonData)";
            string spCreate = "CREATE PROCEDURE " + spName + " (@jsonData json) AS " + tableInsert;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = connection.CreateCommand())
                {
                    //Create Table
                    command.CommandText = tableCreate;
                    await command.ExecuteNonQueryAsync();

                    //Create SP for writing json values
                    command.CommandText = spCreate;
                    await command.ExecuteNonQueryAsync();

                    command.CommandText = tableInsert;
                    var parameter = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                    command.Parameters.Add(parameter);

                    //Test 1
                    //Write json value using a parameterized query
                    parameter.Value = jsonDataString;
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    ValidateRowsAffected(rowsAffected);

                    //Test 2 
                    //Write a SqlString type as json
                    parameter.Value = new SqlString(jsonDataString);
                    int rowsAffected2 = await command.ExecuteNonQueryAsync();
                    ValidateRowsAffected(rowsAffected2);

                    //Test 3
                    //Write json value using SP
                    using (SqlCommand command2 = connection.CreateCommand())
                    {
                        command2.CommandText = spName;
                        command2.CommandType = CommandType.StoredProcedure;
                        var parameter2 = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                        parameter2.Value = jsonDataString;
                        command2.Parameters.Add(parameter2);
                        int rowsAffected3 = await command.ExecuteNonQueryAsync();
                        ValidateRowsAffected(rowsAffected3);
                    }
                }
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.IsJsonSupported))]
        public void TestJsonRead()
        {
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("Json_Test");
            string spName = DataTestUtility.GetUniqueNameForSqlServer("spJson_ReadTest");

            string tableCreate = "CREATE TABLE " + tableName + " (Data json)";
            string tableInsert = "INSERT INTO " + tableName + " VALUES (@jsonData)";
            string tableRead = "SELECT * FROM " + tableName;
            string spCreate = "CREATE PROCEDURE " + spName + "AS " + tableRead;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    //Create Table
                    command.CommandText = tableCreate;
                    command.ExecuteNonQuery();

                    //Create SP for reading from json column
                    command.CommandText = spCreate;
                    command.ExecuteNonQuery();

                    //Insert sample json data
                    //This will be used for reading
                    command.CommandText = tableInsert;
                    var parameter = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                    parameter.Value = jsonDataString;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();

                    //Test 1
                    //Read json value using query
                    command.CommandText = tableRead;
                    var reader = command.ExecuteReader();
                    ValidateRows(reader);

                    //Test 2
                    //Read the column metadata
                    ValidateSchema(reader);
                    reader.Close();

                    //Test 3
                    //Read json value using SP
                    using (SqlCommand command2 = connection.CreateCommand())
                    {
                        command2.CommandText = spName;
                        command2.CommandType = CommandType.StoredProcedure;
                        var reader2 = command2.ExecuteReader();
                        ValidateRows(reader2);
                        reader2.Close();
                    }

                    DataTestUtility.DropTable(connection, tableName);
                }
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.IsJsonSupported))]
        public async Task TestJsonReadAsync()
        {
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("Json_Test");
            string spName = DataTestUtility.GetUniqueNameForSqlServer("spJson_ReadTest");

            string tableCreate = "CREATE TABLE " + tableName + " (Data json)";
            string tableInsert = "INSERT INTO " + tableName + " VALUES (@jsonData)";
            string tableRead = "SELECT * FROM " + tableName;
            string spCreate = "CREATE PROCEDURE " + spName + "AS " + tableRead;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = connection.CreateCommand())
                {
                    //Create Table
                    command.CommandText = tableCreate;
                    await command.ExecuteNonQueryAsync();

                    //Create SP for reading from json column
                    command.CommandText = spCreate;
                    await command.ExecuteNonQueryAsync();

                    //Insert sample json data
                    //This will be used for reading
                    command.CommandText = tableInsert;
                    var parameter = new SqlParameter("@jsonData", SqlDbTypeExtensions.Json);
                    parameter.Value = jsonDataString;
                    command.Parameters.Add(parameter);
                    await command.ExecuteNonQueryAsync();

                    //Test 1
                    //Read json value using query
                    command.CommandText = tableRead;
                    var reader = await command.ExecuteReaderAsync();
                    await ValidateRowsAsync(reader);

                    //Test 2
                    //Read the column metadata
                    ValidateSchema(reader);
                    reader.Close();

                    //Test 3
                    //Read json value using SP
                    using (SqlCommand command2 = connection.CreateCommand())
                    {
                        command2.CommandText = spName;
                        command2.CommandType = CommandType.StoredProcedure;
                        var reader2 = await command2.ExecuteReaderAsync();
                        await ValidateRowsAsync(reader2);
                        reader2.Close();
                    }
                }
            }
        }
    }
}
