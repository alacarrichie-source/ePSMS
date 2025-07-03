//using FluentValidation;
//using FluentValidation.Internal;
//using iLgs.Exceptions;
//using iLgs.Services.Validators;
//using iLgs.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface IValidationService<T>
//    {
//        Task<ServiceResult<T>> ValidateAsync(T model);
//        Task<ServiceResult<T>> ValidateAsync(T model, string ruleSet);
//    }

//    public class ValidationService<T> : IValidationService<T>
//    {
//        private readonly IValidator<T> _validator;

//        // Constructor accepting an instance of the validator
//        //public ValidationService(IValidator<T> validator)
//        //{
//        //    _validator = validator;
//        //}

//        public ValidationService(IValidator<T> validator)
//        {
//            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
//        }


//        //private readonly Type _validatorType;

//        //// Constructor accepting the validator type
//        //public ValidationService(Type validatorType)
//        //{
//        //    _validatorType = validatorType;
//        //}


//        public async Task<ServiceResult<T>> ValidateAsync(T model)
//        {
//            return await ValidateAsync(model, null);
//        }

//        public async Task<ServiceResult<T>> ValidateAsync(T model, string ruleSet)
//        {
//            //var validator = GetValidator<T>();
//            var validator = _validator;

//            var context = new ValidationContext<T>(model);
//            if (ruleSet.Any())
//            {
//                context = new ValidationContext<T>(
//                    model,
//                    new PropertyChain(),
//                    new CombinedValidatorSelector(new[] { ruleSet })
//                );
//            }

//            var validationResult = await validator.ValidateAsync(context);

//            if (!validationResult.IsValid)
//            {
//                var errors = validationResult.Errors
//                    .ToDictionary(
//                        e => Utility.GetDisplayName(typeof(T), e.PropertyName),
//                        e => e.ErrorMessage
//                    );
//                return ServiceResult<T>.Failure(errors);
//            }

//            return ServiceResult<T>.Success(model);
//        }

//        //private IValidator<T> GetValidator<T>()
//        //{
//        //    // Implement a way to resolve the validator, e.g., using dependency injection or creating it directly.
//        //    return (IValidator<T>)Activator.CreateInstance(typeof(T));
//        //}

//        //private IValidator<T> GetValidator<T>()
//        //{
//        //    // Resolve the specific validator by name
//        //    var validatorType = Type.GetType(_validatorName);
//        //    if (validatorType == null)
//        //    {
//        //        throw new ArgumentException($"Validator type '{_validatorName}' not found.");
//        //    }

//        //    return (IValidator<T>)Activator.CreateInstance(validatorType);
//        //}

//        //private IValidator<T> GetValidator<T>()
//        //{
//        //    // Create an instance of the specified validator type
//        //    return (IValidator<T>)Activator.CreateInstance(_validatorType);
//        //}
//    }
//}