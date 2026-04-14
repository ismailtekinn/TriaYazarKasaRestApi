using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Data.Acces.Data;
using TriaYazarKasaRestApi.Data.Acces.Models;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.manager
{
    public class BekoBasketOperationStore : IBekoBasketOperationStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly object _syncRoot = new();

        public BekoBasketOperationStore(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public void Set(BekoBasketOperationStatusDto operation)
        {
            lock (_syncRoot)
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var existing = dbContext.BekoBasketOperations.Find(operation.BasketId);

                if (existing == null)
                {
                    dbContext.BekoBasketOperations.Add(MapToRecord(operation));
                }
                else
                {
                    UpdateRecord(existing, operation);
                }

                dbContext.SaveChanges();
            }
        }

        public BekoBasketOperationStatusDto? Get(string basketId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var operation = dbContext.BekoBasketOperations
                .AsNoTracking()
                .FirstOrDefault(x => x.BasketId == basketId);

            return operation == null ? null : MapToDto(operation);
        }

        public BekoBasketOperationStatusDto AddOrUpdate(string basketId, Func<string, BekoBasketOperationStatusDto> addValueFactory, Func<string, BekoBasketOperationStatusDto, BekoBasketOperationStatusDto> updateValueFactory)
        {
            lock (_syncRoot)
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var existing = dbContext.BekoBasketOperations.Find(basketId);
                BekoBasketOperationStatusDto result;

                if (existing == null)
                {
                    result = addValueFactory(basketId);
                    dbContext.BekoBasketOperations.Add(MapToRecord(result));
                }
                else
                {
                    var current = MapToDto(existing);
                    result = updateValueFactory(basketId, current);
                    UpdateRecord(existing, result);
                }

                dbContext.SaveChanges();
                return result;
            }
        }

        private static BekoBasketOperationRecord MapToRecord(BekoBasketOperationStatusDto dto)
        {
            return new BekoBasketOperationRecord
            {
                BasketId = dto.BasketId,
                OperationId = dto.OperationId,
                StatusCode = dto.StatusCode,
                StatusMessage = dto.StatusMessage,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                IsFinal = dto.IsFinal,
                ReceiptNo = dto.ReceiptResult?.ReceiptNo,
                ZNo = dto.ReceiptResult?.ZNo,
                Uuid = dto.ReceiptResult?.Uuid,
                PaymentsJson = dto.ReceiptResult?.Payments.Count > 0
                    ? JsonSerializer.Serialize(dto.ReceiptResult.Payments, JsonOptions)
                    : null,
                ReceiptResultJson = dto.ReceiptResult == null
                    ? null
                    : JsonSerializer.Serialize(dto.ReceiptResult, JsonOptions)
            };
        }

        private static void UpdateRecord(BekoBasketOperationRecord record, BekoBasketOperationStatusDto dto)
        {
            record.OperationId = dto.OperationId;
            record.StatusCode = dto.StatusCode;
            record.StatusMessage = dto.StatusMessage;
            record.CreatedAtUtc = dto.CreatedAtUtc;
            record.UpdatedAtUtc = dto.UpdatedAtUtc;
            record.IsFinal = dto.IsFinal;
            record.ReceiptNo = dto.ReceiptResult?.ReceiptNo;
            record.ZNo = dto.ReceiptResult?.ZNo;
            record.Uuid = dto.ReceiptResult?.Uuid;
            record.PaymentsJson = dto.ReceiptResult?.Payments.Count > 0
                ? JsonSerializer.Serialize(dto.ReceiptResult.Payments, JsonOptions)
                : null;
            record.ReceiptResultJson = dto.ReceiptResult == null
                ? null
                : JsonSerializer.Serialize(dto.ReceiptResult, JsonOptions);
        }

        private static BekoBasketOperationStatusDto MapToDto(BekoBasketOperationRecord record)
        {
            var receiptResult = DeserializeReceiptResult(record);

            if (receiptResult == null && (record.ReceiptNo.HasValue || record.ZNo.HasValue || !string.IsNullOrWhiteSpace(record.Uuid) || !string.IsNullOrWhiteSpace(record.PaymentsJson)))
            {
                receiptResult = new BekoReceiptResultDto
                {
                    BasketId = record.BasketId,
                    ReceiptNo = record.ReceiptNo,
                    ZNo = record.ZNo,
                    Uuid = record.Uuid,
                    Payments = DeserializePayments(record.PaymentsJson)
                };
            }

            return new BekoBasketOperationStatusDto
            {
                BasketId = record.BasketId,
                OperationId = record.OperationId,
                StatusCode = record.StatusCode,
                StatusMessage = record.StatusMessage,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc,
                IsFinal = record.IsFinal,
                ReceiptResult = receiptResult
            };
        }

        private static BekoReceiptResultDto? DeserializeReceiptResult(BekoBasketOperationRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.ReceiptResultJson))
                return null;

            var receiptResult = JsonSerializer.Deserialize<BekoReceiptResultDto>(record.ReceiptResultJson, JsonOptions);
            if (receiptResult == null)
                return null;

            receiptResult.ReceiptNo ??= record.ReceiptNo;
            receiptResult.ZNo ??= record.ZNo;
            receiptResult.Uuid ??= record.Uuid;
            if (receiptResult.Payments.Count == 0)
                receiptResult.Payments = DeserializePayments(record.PaymentsJson);

            return receiptResult;
        }

        private static List<BekoReceiptPaymentDto> DeserializePayments(string? paymentsJson)
        {
            if (string.IsNullOrWhiteSpace(paymentsJson))
                return new List<BekoReceiptPaymentDto>();

            return JsonSerializer.Deserialize<List<BekoReceiptPaymentDto>>(paymentsJson, JsonOptions)
                ?? new List<BekoReceiptPaymentDto>();
        }
    }
}
