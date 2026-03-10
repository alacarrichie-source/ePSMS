using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcsFromPo
{
    public interface IIcsSharedService
    {
        ValueTask<string> NextRefNoAsync(DateTime parDate, string refType, IcsValue icsValue);
    }

    public class IcsSharedService : IIcsSharedService
    {
        private readonly AppManEntities _db;

        public IcsSharedService(AppManEntities db)
        {
            _db = db;
        }

        public async ValueTask<string> NextRefNoAsync(DateTime parDate, string refType, IcsValue icsValue)
        {
            string icsType = "";
            string yyyy = parDate.Year.ToString().Trim();
            string mm = parDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            if (icsValue == IcsValue.SPLV)
            {
                icsType = "SPLV";
            }
            else
            {
                icsType = "SPHV";
            }

            string keyName = icsType + "-" + yyyy + "-" + mm;

            var data = await _db.IcsPars.Where(w => w.RefType == refType && w.RefNo.StartsWith(icsType) && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            if (data == null)
            {
                return keyName + "-" + "00001";
            }
            else
            {
                var sequence = (int.Parse(data.RefNo.Split('-')[3]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(5, '0');
            }
        }
    }
}