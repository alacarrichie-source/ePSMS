//using iLgs.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace iLgs.Services.Interfaces
//{
//    public interface IParService
//    {
//        IQueryable<PAR_VM> GetAll();
//        IQueryable<PARAcknowledgementVM> GetAcknowledgedOrderItems(Guid? orderItemId);
//        Task<PARAcknowledgementVM> GetAcknowledgedOrderItemByItemId(Guid? parItemId);
//        Task<Models.PAR> GetByIdAsync(Guid parId);
//        Task<Models.PAR> GetByParNoAsync(string parNo);
//        Task<int?> GetRemainingQty(Guid? orderItemId, Guid? parItemId);
//        Task<bool> IsAnyParNoAsync(Guid parId, string parNo);
//        Task<bool> IsPostedAsync(Guid parId);        

//        Task<PAR_VM> CreateAsync(PAR_VM model, string user, DateTime date);        
//        Task<PAR_VM> UpdateAsync(PAR_VM model, string user, DateTime date);       
//        Task<PAR_VM> DeleteAsync(PAR_VM model, string user, DateTime date);
        
//        Task PostAsync(Guid parId, string user, DateTime date);
//        Task UnpostAsync(Guid parId, string user, DateTime date);
//        Task GeneratePAR(GenerateParVM model, string user, DateTime date);

//        Task<PARAcknowledgementVM> CreateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
//        Task<PARAcknowledgementVM> UpdateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
//        Task<PARAcknowledgementVM> DeleteAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
//    }
//}
