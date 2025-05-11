using System.Data.Common;
using Microsoft.Data.SqlClient;
using TutorialWebApp.Exceptions;
using WebApplication1.Model;

namespace WebApplication1.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    public async Task<int> AddProductToWarehouse(AddProductRequest request)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        int newId;

        try
        {
            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.idProduct);
            var doesProductExist = await command.ExecuteScalarAsync();
            if (doesProductExist is null)
            {
                throw new NotFoundException("Produkt o tym id nie istnieje");
            }

            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", request.idWarehouse);

            var doesWarehouseExist = await command.ExecuteScalarAsync();
            if (doesWarehouseExist is null)
            {
                throw new NotFoundException("Magazyn o tym id nie istnieje");
            }

            command.Parameters.Clear();
            command.CommandText = @"SELECT TOP 1 IdOrder, Price FROM [Order] o
                                    JOIN Product p ON o.IdProduct = p.IdProduct
                                    WHERE 
                                   p.IdProduct = @IdProduct AND
                                   Amount = @Amount AND
                                   CreatedAt < @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", request.idProduct);
            command.Parameters.AddWithValue("@Amount", request.amount);
            command.Parameters.AddWithValue("@CreatedAt", request.createdAt);

            int orderId;
            decimal price;
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!reader.Read())
                {
                    throw new NotFoundException("Nie znaleziono zamówienia zakupu produktu w tabeli Order.");
                }

                orderId = reader.GetInt32(0);
                price = reader.GetDecimal(1);
            }


            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Product_Warehouse WHERE 
                                   IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", orderId);
            var idAlreadyFulfilled = await command.ExecuteScalarAsync();
            if (idAlreadyFulfilled is not null)
            {
                throw new ConflictException("Zamówienie zostało juz zrealizowane");
            }

            command.Parameters.Clear();
            command.CommandText = @"UPDATE [Order] SET 
                                   FulfilledAt = @FulfilledAt
                                   WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            command.CommandText = @"INSERT INTO Product_Warehouse (IdWarehouse,IdProduct,IdOrder,Amount,Price,CreatedAt)
                                    OUTPUT INSERTED.IdProductWarehouse
                                    VALUES(@IdWarehouse,@IdProduct,@IdOrder,@Amount,@Total,@CreatedAt)";
            command.Parameters.AddWithValue("@IdWarehouse", request.idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", request.idProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", request.amount);
            command.Parameters.AddWithValue("@Total", price * request.amount);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            newId = (int)command.ExecuteScalar();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return newId;
    }
}