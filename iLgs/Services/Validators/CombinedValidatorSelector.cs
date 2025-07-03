//using FluentValidation;
//using FluentValidation.Internal;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;

//namespace iLgs.Services.Validators
//{
//    public class CombinedValidatorSelector : IValidatorSelector
//    {
//        private readonly IValidatorSelector _defaultSelector;
//        private readonly IValidatorSelector _ruleSetSelector;

//        public CombinedValidatorSelector(string[] ruleSets)
//        {
//            _defaultSelector = new DefaultValidatorSelector();
//            _ruleSetSelector = new RulesetValidatorSelector(ruleSets);
//        }

//        public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context)
//        {
//            return _defaultSelector.CanExecute(rule, propertyPath, context) ||
//                   _ruleSetSelector.CanExecute(rule, propertyPath, context);
//        }
//    }
//}