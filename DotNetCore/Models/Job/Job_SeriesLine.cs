using System;
using System.Linq;
using static DotNetCore.Models.Constants;

namespace DotNetCore.Models
{
    public interface ISeriesLine<T>
    {
        void Init();

        String GenerateDocument(EnumSerialCode SerialCode);

        IRepository<T> Context { get; }
    }

    public class JobSeriesLine<T> : ISeriesLine<T> where T : ApplicationDbContext, new()
    {
        private readonly T _context;
        private static JobSeriesLine<T> _instance;
        public IRepository<T> Context { get; private set; }

        public JobSeriesLine(T context)
        {
            this._context = context;
            Context = new Repository<T>(context);
        }

        public static JobSeriesLine<T> Instance
        {
            get
            {
                return _instance ??= new JobSeriesLine<T>(new T());
            }
        }

        public void Init()
        {
            if (!_context.SeriesLine.Any(m => m.SerialCode == EnumSerialCode.Customer.GetEnumDescription()))
                _context.SeriesLine.AddRange(
                    new ModelSeriesLine() { SerialCode = EnumSerialCode.Customer.GetEnumDescription() },
                    new ModelSeriesLine() { SerialCode = EnumSerialCode.SalesOrder.GetEnumDescription() }
                );
        }

        public String GenerateDocument(EnumSerialCode SerialCode)
        {
            var record = _context.SeriesLine.Where(m => m.SerialCode == SerialCode.GetEnumDescription()).OrderByDescending(m => m.LineNo).FirstOrDefault();
            if (record != null)
            {
                record.RunningNumber += record.IncrementNo;
                record.LastUsedDate = DateTime.Now;
                _context.SeriesLine.Update(record);
            }
            else
                _context.SeriesLine.Add(new ModelSeriesLine() { SerialCode = SerialCode.GetEnumDescription() });

            return record?.LastUsedNo;
        }
    }
}