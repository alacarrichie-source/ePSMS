using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.PropertyCard;
using iLgs.Services.StockCards;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Card
{
    public interface ICardService
    {
        string GetMenuId(int? cardCategory);
        IQueryable<T> GetAll<T>(string userName) where T : class;
        ValueTask<T> GetByIdAsync<T>(Guid id) where T : class;
        ValueTask<T> CreateAsync<T>(T model, string user, DateTime date) where T : class;
        ValueTask<T> UpdateAsync<T>(T model, string user, DateTime date) where T : class;
        ValueTask<T> DeleteAsync<T>(T model, string user, DateTime date) where T : class;
    }

    public class CardService : ICardService
    {
        private readonly AppManEntities _db;        
        private readonly IPropertyCardService _propertyCardService;
        private readonly IStockCardService _stockCardService;

        public CardService(AppManEntities db,
            IPropertyCardService propertyCardService,
            IStockCardService stockCardService)
        {
            _db = db;
            _propertyCardService = propertyCardService;
            _stockCardService = stockCardService;
        }

        public string GetMenuId(int? cardCategory)
        {
            string menuId = "";
            if (cardCategory == (int?)CardCategory.PROPERTY)
            {
                menuId = "card_property";
            }
            else if (cardCategory == (int?)CardCategory.STOCK)
            {
                menuId = "card_stock";
            }
            else if (cardCategory == (int?)CardCategory.SE)
            {
                menuId = "card_se";
            }
            return menuId;
        }

        public IQueryable<T> GetAll<T>(string userName) where T : class
        {
            if (typeof(T) == typeof(PsCardVM))
            {
                return (IQueryable<T>)_propertyCardService.GetAll(userName);
            }
            else if (typeof(T) == typeof(StockCardVM))
            {
                return (IQueryable<T>)_stockCardService.GetAll(userName);
            }

            throw new NotSupportedException($"GetAll does not support type {typeof(T).Name}");
        }

        public async ValueTask<T> GetByIdAsync<T>(Guid id) where T : class
        {
            if (typeof(T) == typeof(PsCardVM))
            {
                var result = await _propertyCardService.GetByIdAsync(id);
                return result as T;
            }
            else if (typeof(T) == typeof(StockCardVM))
            {
                var result = await _stockCardService.GetByIdAsync(id);
                return result as T;
            }

            throw new NotSupportedException($"GetByIdAsync does not support type {typeof(T).Name}");
        }

        public async ValueTask<T> CreateAsync<T>(T model, string user, DateTime date) where T : class
        {
            if (model is PropertyCardVM psCard)
            {
                var result = await _propertyCardService.CreateAsync(psCard, user, date);
                return result as T; 
            }
            else if (model is StockCardVM stockCard)
            {
                var result = await _stockCardService.CreateAsync(stockCard, user, date);
                return result as T; 
            }

            throw new NotSupportedException($"CreateAsync does not support type {typeof(T).Name}");
        }

        public async ValueTask<T> UpdateAsync<T>(T model, string user, DateTime date) where T : class
        {
            if (model is PropertyCardVM psCard)
            {
                var result = await _propertyCardService.UpdateAsync(psCard, user, date);
                return result as T;
            }
            else if (model is StockCardVM stockCard)
            {
                var result = await _stockCardService.UpdateAsync(stockCard, user, date);
                return result as T;
            }

            throw new NotSupportedException($"CreateAsync does not support type {typeof(T).Name}");
        }

        public async ValueTask<T> DeleteAsync<T>(T model, string user, DateTime date) where T : class
        {
            if (model is PropertyCardVM psCard)
            {
                var result = await _propertyCardService.DeleteAsync(psCard, user, date);
                return result as T;
            }
            else if (model is StockCardVM stockCard)
            {
                var result = await _stockCardService.DeleteAsync(stockCard, user, date);
                return result as T;
            }

            throw new NotSupportedException($"CreateAsync does not support type {typeof(T).Name}");
        }
    }
}