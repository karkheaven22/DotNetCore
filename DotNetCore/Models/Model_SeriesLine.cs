using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetCore.Models
{
    [Table("tbl_SeriesLines")]
    public class ModelSeriesLine
    {
        [Timestamp, Column(Order = 0)]
        public byte[] Timestamp { get; set; }

        [Column(Order = 1), MaxLength(20)]
        public string SerialCode { get; set; }

        [Column(Order = 2)]
        public int LineNo { get; set; } = 1000;

        public int IncrementNo { get; set; } = 1;

        [Required(AllowEmptyStrings = true), MaxLength(3)]
        public String Prefix { get; set; } = "";

        public int RunningNumber { get; set; } = 1;

        public int DigitLength { get; set; } = 5;

        [MaxLength(20)]
        public String StartingNo { get; set; }

        [MaxLength(20)]
        public String EndingNo { get; set; }

        [MaxLength(20)]
        public String WarningNo { get; set; }

        public String LastUsedNo => Prefix + RunningNumber.ToString($"D{DigitLength}");

        public DateTime StartingDate { get; set; } = Constants.MinDateTime;

        public DateTime LastUsedDate { get; set; } = Constants.MinDateTime;
    }

    public class SeriesLineEntityConfiguration : IEntityTypeConfiguration<ModelSeriesLine>
    {
        public void Configure(EntityTypeBuilder<ModelSeriesLine> builder)
        {
            builder.HasKey(e => new { e.SerialCode, e.LineNo });
            builder.Property(p => p.Timestamp)
                .IsRowVersion()
                .IsRequired();
        }
    }
}
