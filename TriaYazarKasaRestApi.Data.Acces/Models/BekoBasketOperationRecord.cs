using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TriaYazarKasaRestApi.Data.Acces.Models
{
    [Table("BekoBasketOperations")]
    public class BekoBasketOperationRecord
    {
        [Key]
        [MaxLength(200)]
        public string BasketId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string OperationId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string StatusCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string StatusMessage { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public bool IsFinal { get; set; }

        public int? ReceiptNo { get; set; }

        public int? ZNo { get; set; }

        [MaxLength(100)]
        public string? Uuid { get; set; }

        public string? PaymentsJson { get; set; }

        public string? ReceiptResultJson { get; set; }
    }
}
