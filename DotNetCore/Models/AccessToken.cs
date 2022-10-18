using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DotNetCore
{
    public sealed class AccessToken
    {
        public string Token { get; }
        public int ExpiresIn { get; }

        public AccessToken(string token, int expiresIn)
        {
            Token = token;
            ExpiresIn = expiresIn;
        }
    }

    public class LoginResponse
    {
        public AccessToken AccessToken { get; }
        public string RefreshToken { get; }

        public LoginResponse(AccessToken accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }

    public enum TransactionStatus
    {
        Approved = 'A',
        Rejected = 'R',
        Done = 'D',
        Failed = 'F',
        Finished = 'S'
    }

    [Table("tbl_transaction")]
    public class Transaction
    {
        [Key, Required, MaxLength(50)]
        public string TransactionId { get; set; }

        [Required, Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string CurrencyCode { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public TransactionStatus Status { get; set; }
    }

    public class ViewTransaction
    {
        public string id { get; set; }
        public string payment { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        public TransactionStatus StatusEnum { get; set; }

        public string Status => ((char)(int)StatusEnum).ToString();
    }
}