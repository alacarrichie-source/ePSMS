//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using iLgs.Models;

//namespace iLgs.Services.Interfaces
//{
    
//    public interface IExceptionService<T> where T : class
//    {

//        //ValueTask TryCatch(Func<ValueTask> nonReturningFunction);
//        //ValueTask TryCatch(Func<ValueTask> nonReturningFunction);
//        //ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction);
//        //ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunctionAsync);
//        //IQueryable<T> TryCatch(Func<IQueryable<T>> returningQueryableFunction);
//        //List<T> TryCatch(Func<List<T>> returningQueryableFunction);

//        ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction);
//        IQueryable<T> TryCatch(Func<IQueryable<T>> returningQueryableFunction);


//    }
//}