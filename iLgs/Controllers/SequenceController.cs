using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class SequenceController : Controller
    {
        private readonly AppManEntities _db;

        public SequenceController()
        {
            _db = new AppManEntities();
        }

        public async Task<string> NextPoNo(DateTime poDate)
        {
            string yy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();
            yy = yy.Substring(2, 2);
            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');
            string keyName = yy + mm;
            var sequence = await _db.Sequences.Where(w => w.KeyName == "PO-NO" + keyName).SingleOrDefaultAsync();
            if (sequence == null)
            {
                sequence = new Sequence()
                {
                    KeyName = "PO-NO" + keyName,
                    KeyValue = "1"
                };
                _db.Sequences.Add(sequence);
            }
            else
            {
                sequence.KeyValue = (decimal.Parse(sequence.KeyValue) + 1).ToString();
            }
            await _db.SaveChangesAsync();
            return keyName + sequence.KeyValue.PadLeft(6, '0');
        }
    }
}