using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Exceptions
{
    public class Xeption : Exception
    {
        public Xeption()
        {
        }

        public Xeption(string message)
            : base(message)
        {
        }

        public Xeption(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public Xeption(Exception innerException, IDictionary data)
            : base(innerException.Message, innerException)
        {
            AddData(data);
        }

        public Xeption(string message, Exception innerException, IDictionary data)
            : base(message, innerException)
        {
            AddData(data);
        }

        public void UpsertDataList(string key, string value)
        {
            if (Data.Contains(key))
            {
                (Data[key] as List<string>)?.Add(value);
                return;
            }

            Data.Add(key, new List<string> { value });
        }

        public void ThrowIfContainsErrors()
        {
            if (Data.Count > 0)
            {
                throw this;
            }
        }

        public void AddData(IDictionary dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return;
            }

            foreach (DictionaryEntry item in dictionary)
            {
                Data.Add(item.Key, item.Value);
            }
        }

        public void AddData(string key, params string[] values)
        {
            Data.Add(key, values);
        }        
    }
}